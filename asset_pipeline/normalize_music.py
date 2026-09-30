"""Normalize the generated suite reproducibly; retain originals in the ignored cache.

Requires imageio-ffmpeg. Run from any directory with Python.
Two passes target -20 LUFS / -2 dBTP, then measure the encoded result.
"""
import json
from pathlib import Path
import re
import shutil
import subprocess
from concurrent.futures import ThreadPoolExecutor
import imageio_ffmpeg

ROOT = Path(__file__).resolve().parents[1]
AUDIO = ROOT / 'unity_cosmic_engine/Assets/Audio'
CACHE = ROOT / 'asset_pipeline/.cache/music_originals'
CACHE.mkdir(parents=True, exist_ok=True)
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
TARGET = 'loudnorm=I=-20:TP=-2:LRA=11'


def run(*args):
    return subprocess.run([FFMPEG, '-hide_banner', '-nostdin', *map(str, args)],
                          capture_output=True, text=True, check=True).stderr


def measure(path):
    log = run('-i', path, '-af', TARGET + ':print_format=json', '-f', 'null', '-')
    return json.loads(re.findall(r'\{\s*"input_i".*?\}', log, re.S)[-1])


def normalize(track):
    dest = AUDIO / track['file']
    source = CACHE / dest.name
    if not source.exists():
        shutil.copy2(dest, source)
    before = measure(source)
    settings = TARGET + ':linear=true:print_format=json'
    for key, value in [('measured_I','input_i'), ('measured_LRA','input_lra'),
                       ('measured_TP','input_tp'), ('measured_thresh','input_thresh'), ('offset','target_offset')]:
        settings += ':' + key + '=' + before[value]
    intermediate = CACHE / (dest.stem + '_leveled.wav')
    run('-y', '-i', source, '-af', settings, '-ar', '44100', '-c:a', 'pcm_f32le', intermediate)
    measured = measure(intermediate)
    # A long quiet intro/tail can move loudnorm's second-pass gate. Correct the
    # integrated level with a constant trim, bounded by the measured true peak.
    trim = min(-20 - float(measured['input_i']), -2.3 - float(measured['input_tp']))
    run('-y', '-i', intermediate, '-af', f'volume={trim}dB', '-c:a', 'libmp3lame', '-b:a', '192k', dest)
    after = measure(dest)
    probe = run('-i', dest, '-f', 'null', '-')
    duration = re.search(r'Duration: (\d+):(\d+):([\d.]+)', probe)
    seconds = int(duration[1])*3600 + int(duration[2])*60 + float(duration[3])
    # Allow one LU when the peak ceiling limits the final constant trim.
    assert abs(float(after['input_i']) + 20) < 1, (dest, after)
    assert float(after['input_tp']) <= -1.5, (dest, after)
    assert 179 <= seconds <= 181, (dest, seconds)
    result = dict(file=dest.name, durationSeconds=seconds, before=before, normalized=after)
    print(dest.name, 'LUFS', after['input_i'], 'dBTP', after['input_tp'], flush=True)
    return result


if __name__ == '__main__':
    tracks = json.loads((ROOT / 'asset_pipeline/music_manifest.json').read_text())['tracks']
    with ThreadPoolExecutor(max_workers=2) as pool:
        results = list(pool.map(normalize, tracks))
    output = ROOT / 'visual_tests/gallery-expansion/audio-validation.json'
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(results, indent=2))
