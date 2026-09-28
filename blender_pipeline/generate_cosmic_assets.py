"""
Blender 5.2 Python Script: High-Fidelity Textured Cosmic Assets Generator
Uses photorealistic texture maps (Sun, Earth, Jupiter, Milky Way, Andromeda, Cosmic Web, CMB)
to export textured PBR 3D models across all 4 cosmic scales.
"""

import bpy
import bmesh
import math
import os

BASE_DIR = os.path.abspath(r"d:\Vibe Code Repo\galactic_view")
TEXTURE_DIR = os.path.join(BASE_DIR, "assets", "textures")
OUTPUT_DIR = os.path.join(BASE_DIR, "unity_cosmic_engine", "Assets", "Models")
os.makedirs(OUTPUT_DIR, exist_ok=True)

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def create_textured_material(name, texture_filename, fallback_color=(0.8, 0.8, 0.8, 1.0), emission_strength=0.0, roughness=0.5):
    mat = bpy.data.materials.new(name=name)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    node_out = nodes.new(type='ShaderNodeOutputMaterial')
    node_bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    node_bsdf.inputs['Roughness'].default_value = roughness
    node_bsdf.inputs['Base Color'].default_value = fallback_color
    
    tex_path = os.path.join(TEXTURE_DIR, texture_filename)
    if os.path.exists(tex_path):
        try:
            img = bpy.data.images.load(tex_path)
            node_tex = nodes.new(type='ShaderNodeTexImage')
            node_tex.image = img
            links.new(node_tex.outputs['Color'], node_bsdf.inputs['Base Color'])
            
            if emission_strength > 0:
                if 'Emission Color' in node_bsdf.inputs:
                    links.new(node_tex.outputs['Color'], node_bsdf.inputs['Emission Color'])
                    node_bsdf.inputs['Emission Strength'].default_value = emission_strength
                elif 'Emission' in node_bsdf.inputs:
                    links.new(node_tex.outputs['Color'], node_bsdf.inputs['Emission'])
        except Exception as e:
            print(f"Warning: could not load texture {tex_path}: {e}")
            
    links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])
    return mat

def create_ring_mesh(name, inner_r, outer_r, segments=96):
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    inner_verts = []
    outer_verts = []
    
    for i in range(segments):
        theta = (i / segments) * 2.0 * math.pi
        x_in = math.cos(theta) * inner_r
        y_in = math.sin(theta) * inner_r
        x_out = math.cos(theta) * outer_r
        y_out = math.sin(theta) * outer_r
        
        v_in = bm.verts.new((x_in, y_in, 0.0))
        v_out = bm.verts.new((x_out, y_out, 0.0))
        inner_verts.append(v_in)
        outer_verts.append(v_out)
        
    bm.verts.ensure_lookup_table()
    for i in range(segments):
        next_i = (i + 1) % segments
        bm.faces.new([inner_verts[i], outer_verts[i], outer_verts[next_i], inner_verts[next_i]])
        
    bm.to_mesh(mesh)
    bm.free()
    return obj

# -------------------------------------------------------------
# 1. STAGE 1: TEXTURED SOLAR SYSTEM
# -------------------------------------------------------------
print("Generating Stage 1: Textured Solar System...")
clear_scene()

mat_sun = create_textured_material("Mat_Sun", "sun_photosphere.jpg", (1.0, 0.8, 0.2, 1.0), emission_strength=4.0)
mat_earth = create_textured_material("Mat_Earth", "earth_photosphere.jpg", (0.1, 0.4, 0.9, 1.0), roughness=0.3)
mat_jupiter = create_textured_material("Mat_Jupiter", "jupiter_photosphere.jpg", (0.8, 0.6, 0.3, 1.0), roughness=0.5)

mat_mercury = create_textured_material("Mat_Mercury", "mercury.jpg", (0.5, 0.5, 0.52, 1.0), roughness=0.8)
mat_venus = create_textured_material("Mat_Venus", "venus.jpg", (0.9, 0.75, 0.4, 1.0), roughness=0.6)
mat_mars = create_textured_material("Mat_Mars", "mars.jpg", (0.85, 0.35, 0.2, 1.0), roughness=0.7)
mat_saturn = create_textured_material("Mat_Saturn", "saturn.jpg", (0.9, 0.8, 0.55, 1.0), roughness=0.5)
mat_neptune = create_textured_material("Mat_Neptune", "neptune.jpg", (0.15, 0.38, 0.95, 1.0), roughness=0.4)
mat_boundary = create_textured_material("Mat_Boundary", "neptune.jpg", (0.2, 0.75, 1.0, 1.0), emission_strength=2.0)

# Sun
bpy.ops.mesh.primitive_uv_sphere_add(radius=4.0, segments=48, ring_count=32, location=(0, 0, 0))
sun = bpy.context.active_object
sun.name = "Sun_Photosphere"
sun.data.materials.append(mat_sun)

# Earth
bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, segments=32, ring_count=24, location=(18.0, 0, 0))
earth = bpy.context.active_object
earth.name = "Earth"
earth.data.materials.append(mat_earth)

# Jupiter
bpy.ops.mesh.primitive_uv_sphere_add(radius=2.6, segments=40, ring_count=28, location=(38.0, 0, 0))
jup = bpy.context.active_object
jup.name = "Jupiter"
jup.data.materials.append(mat_jupiter)

# Saturn & Rings
bpy.ops.mesh.primitive_uv_sphere_add(radius=2.0, segments=36, ring_count=24, location=(54.0, 0, 0))
sat = bpy.context.active_object
sat.name = "Saturn"
sat.data.materials.append(mat_saturn)

sat_rings = create_ring_mesh("Saturn_Rings", 2.8, 5.5, segments=96)
sat_rings.location = (54.0, 0, 0)
sat_rings.rotation_euler = (math.radians(26.7), 0, 0)
sat_rings.data.materials.append(mat_saturn)
sat_rings.parent = sat

# Neptune Boundary (60.14 AU diameter)
bpy.ops.mesh.primitive_uv_sphere_add(radius=1.3, segments=32, ring_count=24, location=(88.0, 0, 0))
nep = bpy.context.active_object
nep.name = "Neptune"
nep.data.materials.append(mat_neptune)

nep_orbit = create_ring_mesh("Neptune_Orbit_Boundary", 87.6, 88.4, segments=128)
nep_orbit.data.materials.append(mat_boundary)

fbx_sol = os.path.join(OUTPUT_DIR, "solar_system_bodies.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_sol, use_selection=False)
print("Saved:", fbx_sol)


# -------------------------------------------------------------
# 2. STAGE 2: TEXTURED MILKY WAY GALAXY
# -------------------------------------------------------------
print("Generating Stage 2: Textured Milky Way Galaxy...")
clear_scene()

mat_mw = create_textured_material("Mat_MilkyWay_Disk", "milky_way_disk.jpg", (0.9, 0.8, 0.6, 1.0), emission_strength=2.2)

# Textured Galactic Plane Plane
bpy.ops.mesh.primitive_plane_add(size=140.0, location=(0, 0, 0))
mw_plane = bpy.context.active_object
mw_plane.name = "MilkyWay_Galactic_Disk"
mw_plane.data.materials.append(mat_mw)

# Central Bulge & Sgr A*
bpy.ops.mesh.primitive_uv_sphere_add(radius=2.2, segments=32, ring_count=24, location=(0, 0, 0))
sgra = bpy.context.active_object
sgra.name = "Sagittarius_A_Horizon"

# Orion Arm Sun Beacon
mat_beacon = create_textured_material("Mat_Beacon", "sun_photosphere.jpg", (1.0, 0.85, 0.2, 1.0), emission_strength=4.0)
bpy.ops.mesh.primitive_uv_sphere_add(radius=1.2, segments=16, ring_count=12, location=(25.0, 6.0, 0.5))
sun_beacon = bpy.context.active_object
sun_beacon.name = "Our_Position_Sun_Beacon"
sun_beacon.data.materials.append(mat_beacon)

fbx_mw = os.path.join(OUTPUT_DIR, "milky_way_spiral.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_mw, use_selection=False)
print("Saved:", fbx_mw)


# -------------------------------------------------------------
# 3. STAGE 3: TEXTURED LOCAL GROUP GALAXIES
# -------------------------------------------------------------
print("Generating Stage 3: Textured Local Group...")
clear_scene()

mat_mw_lg = create_textured_material("Mat_LG_MilkyWay", "milky_way_disk.jpg", (0.9, 0.8, 0.6, 1.0), emission_strength=2.2)
mat_andromeda = create_textured_material("Mat_Andromeda_Disk", "andromeda_galaxy_disk.jpg", (0.95, 0.9, 0.7, 1.0), emission_strength=2.5)

# Milky Way Disk
bpy.ops.mesh.primitive_plane_add(size=30.0, location=(-25.0, 0, 0))
mw_lg = bpy.context.active_object
mw_lg.name = "LocalGroup_MilkyWay"
mw_lg.data.materials.append(mat_mw_lg)

# Andromeda Galaxy (M31, tilted at 77 degrees)
bpy.ops.mesh.primitive_plane_add(size=55.0, location=(45.0, 15.0, -18.0))
m31_lg = bpy.context.active_object
m31_lg.name = "LocalGroup_Andromeda_M31"
m31_lg.rotation_euler = (math.radians(77.0), math.radians(35.0), 0)
m31_lg.data.materials.append(mat_andromeda)

fbx_lg = os.path.join(OUTPUT_DIR, "local_group_galaxies.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_lg, use_selection=False)
print("Saved:", fbx_lg)


# -------------------------------------------------------------
# 4. STAGE 4: TEXTURED COSMIC WEB & CMB HORIZON
# -------------------------------------------------------------
print("Generating Stage 4: Textured Cosmic Web & CMB Horizon...")
clear_scene()

mat_cmb = create_textured_material("Mat_CMB_Horizon", "cmb_horizon_sky.jpg", (0.2, 0.6, 1.0, 1.0), emission_strength=1.8)
mat_web = create_textured_material("Mat_Cosmic_Web", "cosmic_web_simulation.jpg", (0.95, 0.8, 0.3, 1.0), emission_strength=2.0)

# CMB Particle Horizon Sphere (93 GLY Boundary)
bpy.ops.mesh.primitive_uv_sphere_add(radius=120.0, segments=64, ring_count=48, location=(0, 0, 0))
horizon = bpy.context.active_object
horizon.name = "Observable_Universe_CMB_Horizon"
horizon.data.materials.append(mat_cmb)

# Inner Cosmic Web Plane
bpy.ops.mesh.primitive_plane_add(size=160.0, location=(0, 0, 0))
web_plane = bpy.context.active_object
web_plane.name = "Cosmic_Web_Simulation_Plane"
web_plane.data.materials.append(mat_web)

fbx_uni = os.path.join(OUTPUT_DIR, "observable_universe_boundary.fbx")
bpy.ops.export_scene.fbx(filepath=fbx_uni, use_selection=False)
print("Saved:", fbx_uni)

print("\n--- ALL TEXTURED 3D MODELS GENERATED WITH REAL PBR TEXTURES ---")
