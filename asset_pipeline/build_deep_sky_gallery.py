"""Build the offline gallery from attributed astronomical images and OpenNGC coordinates.

Run: python asset_pipeline/build_deep_sky_gallery.py
Requires requests, beautifulsoup4 and Pillow. Cached source responses make reruns resumable.
"""
import concurrent.futures as futures
import csv
import hashlib
import io
import json
import re
import time
import threading
from pathlib import Path
from urllib.parse import urlparse

import requests
from bs4 import BeautifulSoup
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / 'asset_pipeline/.cache'
OUTPUT = ROOT / 'unity_cosmic_engine/Assets/StreamingAssets/Gallery'
MANIFEST = ROOT / 'unity_cosmic_engine/Assets/Resources/DeepSkyCatalog.json'
NASA = 'https://science.nasa.gov/mission/hubble/science/explore-the-night-sky/'
OPENNGC_REVISION = '75ca7ff090e1d0081a5b08be70eb3bc45ccd9e06'
OPENNGC = f'https://raw.githubusercontent.com/mattiaverga/OpenNGC/{OPENNGC_REVISION}/database_files/'
NASA_POLICY = 'https://www.nasa.gov/nasa-brand-center/images-and-media/'
HEADERS = {'User-Agent': 'AstronauticaCosmicExplorer/1.0 (educational astronomy gallery)'}
COMMONS_LOCK = threading.Lock()
for p in [CACHE, OUTPUT / 'images', OUTPUT / 'thumbs', MANIFEST.parent]:
    p.mkdir(parents=True, exist_ok=True)


def get(url):
    path = CACHE / hashlib.sha256(url.encode()).hexdigest()
    if path.exists():
        return path.read_bytes()
    for attempt in range(4):
        try:
            response = requests.get(url, headers=HEADERS, timeout=45)
            response.raise_for_status()
            path.write_bytes(response.content)
            return response.content
        except requests.RequestException:
            if attempt == 3:
                raise
            time.sleep(2 ** attempt)


def clean(html):
    return BeautifulSoup(html or '', 'html.parser').get_text(' ', strip=True)


def sexagesimal(value):
    sign = -1 if value.startswith('-') else 1
    parts = [float(x) for x in value.lstrip('+-').split(':')]
    return sign * (parts[0] + parts[1] / 60 + parts[2] / 3600)


def nasa_image(url):
    soup = BeautifulSoup(get(url), 'html.parser')
    for figure in soup.select('main figure'):
        image = figure.find('img')
        if not image or not image.get('src'):
            continue
        if any(x in image.get('alt', '').lower() for x in ['star chart', 'star map', 'skymap', 'portrait']):
            continue
        wrapper = figure.parent
        credit = wrapper.select_one('.hds-credits')
        if credit is None:
            continue
        return dict(imageUrl=image['src'], imageCredit=credit.get_text(' ', strip=True),
                    imageSourceUrl=url, imageLicense='NASA / Hubble image-use guidelines; retain the full credit',
                    imageLicenseUrl=NASA_POLICY, imageAlt=image.get('alt', ''), imageProvider='NASA / Hubble')
    raise ValueError('No credited telescope image on ' + url)


def commons_image(number, ngc):
    params = dict(action='query', format='json', generator='categorymembers', gcmtitle=f'Category:Messier {number}',
                  gcmnamespace=6, gcmlimit=50, prop='imageinfo', iiprop='url|extmetadata', iiurlwidth=960)
    url = requests.Request('GET', 'https://commons.wikimedia.org/w/api.php', params=params).prepare().url
    with COMMONS_LOCK:
        data = json.loads(get(url))
        time.sleep(1.5)
    candidates = []
    for page in data.get('query', {}).get('pages', {}).values():
        info = page.get('imageinfo', [{}])[0]
        meta = info.get('extmetadata', {})
        license_name = clean(meta.get('LicenseShortName', {}).get('value', ''))
        title = page['title']
        if not re.search(rf'(?:Messier[ _-]?0*{number}|\bM0*{number})(?!\d)', title, re.I):
            continue
        if any(x in title.lower() for x in ['map', '.svg', 'chart', 'drawing', 'sketch', 'finder', 'location']):
            continue
        if len(re.findall(r'Messier[ _-]?\d+', title, re.I)) > 1 or 'stellarium' in title.lower():
            continue
        if not (license_name.startswith('CC') or license_name in ['Public domain', 'Attribution']):
            continue
        if not re.search(r'\.(jpg|jpeg|png)(?:\?|$)', info.get('url', ''), re.I):
            continue
        artist = clean(meta.get('Artist', {}).get('value', ''))
        desc = clean(meta.get('ImageDescription', {}).get('value', ''))
        source = clean(meta.get('Credit', {}).get('value', ''))
        authority = bool(re.search(r'NASA|ESO|NOIRLab|NOAO|ESA|JPL', artist + source, re.I))
        score = (150 if 'noao-' in title else 100 if authority else 80 if '2mass' in title.lower() else 0) - page.get('index', 0)
        noao = re.search(r'\((noao-[^)]+)\)', title)
        if noao:
            source_page = 'https://noirlab.edu/public/images/' + noao.group(1) + '/'
            candidates.append((score, observatory_image(source_page, 'NOIRLab', 'https://noirlab.edu/public/copyright/')))
            continue
        candidates.append((score, dict(imageUrl=info.get('thumburl', info['url']),
                            imageCredit=artist or clean(meta.get('Attribution', {}).get('value', '')),
                            imageSourceUrl=info['descriptionurl'], imageLicense=license_name,
                            imageLicenseUrl=meta.get('LicenseUrl', {}).get('value', '') or 'https://commons.wikimedia.org/wiki/Commons:Reusing_content_outside_Wikimedia',
                            imageAlt=title.removeprefix('File:'), imageProvider='Public telescope archive / Wikimedia Commons')))
    if not candidates:
        raise ValueError(f'No licensed matching image for M{number}')
    return max(candidates, key=lambda x: x[0])[1]


def observatory_image(url, provider, terms):
    soup = BeautifulSoup(get(url), 'html.parser')
    image_id = urlparse(url).path.strip('/').split('/')[-1]
    image = next(a['href'] for a in soup.select('a[href]')
                 if '/screen/' in a['href'] and Path(urlparse(a['href']).path).stem == image_id)
    credit = soup.select_one('.credit').get_text(' ', strip=True)
    return dict(imageUrl=image, imageCredit=credit, imageSourceUrl=url, imageLicense='CC BY 4.0',
                imageLicenseUrl=terms, imageAlt=soup.find('h1').get_text(' ', strip=True), imageProvider=provider)


def write_image(entry):
    filename = entry['id'].replace(' ', '_') + '.jpg'
    image = Image.open(io.BytesIO(get(entry['imageUrl']))).convert('RGB')
    if min(image.size) < 100:
        raise ValueError('Image is too small: ' + entry['id'])
    image.thumbnail((1280, 1280), Image.Resampling.LANCZOS)
    image.save(OUTPUT / 'images' / filename, quality=90, optimize=True)
    image.thumbnail((320, 240), Image.Resampling.LANCZOS)
    image.save(OUTPUT / 'thumbs' / filename, quality=85, optimize=True)
    entry['imageFile'] = filename
    entry['imageProcessing'] = 'Resized, aspect ratio preserved; JPEG conversion. No generative alterations.'


def main():
    rows = []
    for fn in ['NGC.csv', 'addendum.csv']:
        rows.extend(csv.DictReader(io.StringIO(get(OPENNGC + fn).decode()), delimiter=';'))
    by_name = {r['Name']: r for r in rows}
    messier = {int(n): r for r in rows for n in r['M'].split(',') if n}
    messier[102] = by_name['NGC5866']  # Traditional M102 identification; retained explicitly below.
    assert set(messier) == set(range(1, 111))
    home = BeautifulSoup(get(NASA + 'hubble-messier-catalog/'), 'html.parser')
    nasa_pages = {}
    for a in home.select('a[href]'):
        match = re.search(r'/messier-(\d+)/', a['href'])
        if match:
            nasa_pages[int(match[1])] = a['href']
    kinds = {'G': (12, 'Galaxy'), 'GCl': (6, 'Globular cluster'), 'OCl': (7, 'Open cluster'),
             'HII': (3, 'Emission nebula'), 'Cl+N': (3, 'Cluster and nebula'), 'EmN': (3, 'Emission nebula'),
             'RfN': (11, 'Reflection nebula'), 'DrkN': (11, 'Dark nebula'), 'Neb': (11, 'Nebula'),
             'PN': (4, 'Planetary nebula'), 'SNR': (5, 'Supernova remnant'), '**': (9, 'Double star'),
             '*Ass': (10, 'Star cloud / asterism')}
    curated = [('NGC7000','North America Nebula',20), ('NGC7293','Helix Nebula',63),
               ('NGC3372','Carina Nebula',92), ('NGC0104','47 Tucanae',106),
               ('NGC5139','Omega Centauri',80), ('NGC5128','Centaurus A',77),
               ('NGC0869','Double Cluster: NGC 869',14), ('NGC6960','Western Veil Nebula',34),
               ('NGC7635','Bubble Nebula',11), ('NGC2237','Rosette Nebula',49),
               ('NGC2070','Tarantula Nebula',103), ('NGC2392','NGC 2392',39)]
    items = [(f'M{n}', r, None, nasa_pages.get(n)) for n, r in sorted(messier.items())]
    items.extend((key, by_name[key], name, NASA + f'hubble-caldwell-catalog/caldwell-{c}/') for key,name,c in curated)
    overrides_path = ROOT / 'asset_pipeline/gallery_sources.json'
    overrides = json.loads(overrides_path.read_text()) if overrides_path.exists() else {}
    preferred_names = {'M11': 'Wild Duck Cluster', 'M17': 'Omega Nebula',
                       'M24': 'Small Sagittarius Star Cloud', 'M76': 'Little Dumbbell Nebula'}

    def process(item):
        key, row, name, page = item
        kind, label = kinds.get(row['Type'], (11, 'Deep-sky object'))
        entry = dict(id=key, commonName=name or preferred_names.get(key) or row['Common names'].split(',')[0] or f'Messier {int(key[1:])}',
                     aliases=row['Common names'],
                     ngcOrAlt=re.sub(r'^(NGC|IC)0*(\d+)', r'\1 \2', row['Name']), objectType=kind, objectClass=label, constellation=row['Const'],
                     raHours=sexagesimal(row['RA']), decDegrees=sexagesimal(row['Dec']),
                     apparentMag=float(row['V-Mag']) if row['V-Mag'] else 0, magnitudeKnown=bool(row['V-Mag']),
                     coordinateEpoch='J2000', catalogSourceUrl='https://github.com/mattiaverga/OpenNGC',
                     catalogLicense='CC BY-SA 4.0 / OpenNGC',
                     description=f"{label} in {row['Const']}. Catalog designation: {row['Name']}.")
        if key == 'M102':
            entry['description'] += ' M102 is traditionally identified with NGC 5866; its historical identification is disputed.'
        if key in overrides:
            source = overrides[key]
            entry.update(observatory_image(source['url'], source['provider'], source['terms']))
        elif key == 'NGC2237':
            entry.update(observatory_image('https://www.eso.org/public/images/b12/', 'ESO', 'https://www.eso.org/public/outreach/copyright/'))
        elif page:
            entry.update(nasa_image(page))
        else:
            entry.update(commons_image(int(key[1:]), row['Name']))
        if not entry['imageCredit']:
            raise ValueError('Missing credit: ' + key)
        write_image(entry)
        print(key, entry['imageProvider'], flush=True)
        return entry

    results, failures = [], []
    with futures.ThreadPoolExecutor(max_workers=4) as pool:
        pending = {pool.submit(process, item): item[0] for item in items}
        for future in futures.as_completed(pending):
            try:
                results.append(future.result())
            except Exception as exc:
                failures.append({'id':pending[future], 'error':str(exc)})
                print('FAILED', pending[future], str(exc), flush=True)
    order = {item[0]: i for i,item in enumerate(items)}
    results.sort(key=lambda entry: order[entry['id']])
    MANIFEST.write_text(json.dumps({'entries':results}, indent=2, ensure_ascii=False), encoding='utf-8')
    credits = ['ASTRONAUTICA TELESCOPE IMAGE CREDITS', '',
               'Images resized with aspect ratio preserved and converted to JPEG. No generative alterations.',
               'Some images show a detail or surrounding field, rather than the entire catalog object.', '',
               'Catalog data: OpenNGC, https://github.com/mattiaverga/OpenNGC',
               'Adapted subset: CC BY-SA 4.0, https://creativecommons.org/licenses/by-sa/4.0/',
               'Changes: selection, decimal coordinates, class labels, M102 traditional NGC 5866 identification.', '',
               'Per-image terms apply independently from the catalog-data license.', '']
    for entry in results:
        credits.extend([entry['id'] + ' / ' + entry['commonName'], entry['imageCredit'],
                        entry['imageSourceUrl'], entry['imageLicense'], entry['imageLicenseUrl'], ''])
    (OUTPUT / 'CREDITS.txt').write_text('\n'.join(credits), encoding='utf-8')
    (ROOT / 'asset_pipeline/gallery_failures.json').write_text(json.dumps(failures, indent=2))
    print('COMPLETE',len(results),'of',len(items),'failures',len(failures),flush=True)
    if failures:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
