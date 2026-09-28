"""
Blender 5.2 Python Pipeline: Complete Unified Cosmic Zoom Engine Project
Generates 'cosmic_zoom_universe.blend' containing all 4 cosmic scales, PBR textures,
cameras, and collections for immediate interactive exploration inside native Blender 5.2.
"""

import bpy
import bmesh
import math
import os

BASE_DIR = os.path.abspath(r"d:\Vibe Code Repo\galactic_view")
TEXTURE_DIR = os.path.join(BASE_DIR, "assets", "textures")
BLEND_OUTPUT = os.path.join(BASE_DIR, "cosmic_zoom_universe.blend")

# 1. Reset and initialize scene
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.name = "Cosmic_Zoom_Engine"

# Set world background to deep cosmic black
world = bpy.data.worlds.new("Cosmic_Deep_Space")
scene.world = world
world.use_nodes = True
bg_node = world.node_tree.nodes.get("Background")
if bg_node:
    bg_node.inputs["Color"].default_value = (0.005, 0.008, 0.015, 1.0)
    bg_node.inputs["Strength"].default_value = 0.5

def create_pbr_material(name, texture_filename=None, color=(0.8, 0.8, 0.8, 1.0), emission=0.0, roughness=0.5):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    out = nodes.new(type='ShaderNodeOutputMaterial')
    bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Base Color'].default_value = color
    
    if texture_filename:
        tex_path = os.path.join(TEXTURE_DIR, texture_filename)
        if os.path.exists(tex_path):
            img = bpy.data.images.load(tex_path)
            tex_node = nodes.new(type='ShaderNodeTexImage')
            tex_node.image = img
            links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])
            
            if emission > 0:
                if 'Emission Color' in bsdf.inputs:
                    links.new(tex_node.outputs['Color'], bsdf.inputs['Emission Color'])
                    bsdf.inputs['Emission Strength'].default_value = emission
                elif 'Emission' in bsdf.inputs:
                    links.new(tex_node.outputs['Color'], bsdf.inputs['Emission'])
    elif emission > 0:
        if 'Emission Color' in bsdf.inputs:
            bsdf.inputs['Emission Color'].default_value = color
            bsdf.inputs['Emission Strength'].default_value = emission
            
    links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return mat

def create_orbit_ring(name, radius, collection, color=(0.22, 0.75, 1.0, 1.0)):
    curve_data = bpy.data.curves.new(name=name + "_Curve", type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.bevel_depth = 0.04
    curve_data.bevel_resolution = 4
    
    spline = curve_data.splines.new('BEZIER')
    spline.bezier_points.add(3)
    
    coords = [
        (radius, 0, 0),
        (0, radius, 0),
        (-radius, 0, 0),
        (0, -radius, 0)
    ]
    r = radius * 0.55228475
    handles_left = [
        (radius, -r, 0),
        (r, radius, 0),
        (-radius, r, 0),
        (-r, -radius, 0)
    ]
    handles_right = [
        (radius, r, 0),
        (-r, radius, 0),
        (-radius, -r, 0),
        (r, -radius, 0)
    ]
    for i in range(4):
        pt = spline.bezier_points[i]
        pt.co = coords[i]
        pt.handle_left = handles_left[i]
        pt.handle_right = handles_right[i]
    spline.use_cyclic_u = True
    
    obj = bpy.data.objects.new(name, curve_data)
    collection.objects.link(obj)
    
    mat = create_pbr_material(name + "_Mat", color=color, emission=1.5)
    obj.data.materials.append(mat)
    return obj

# Collections for each stage
col_stage1 = bpy.data.collections.new("Stage 1 - Solar System (~8.33 LH)")
col_stage2 = bpy.data.collections.new("Stage 2 - Milky Way Galaxy (~100,000 LY)")
col_stage3 = bpy.data.collections.new("Stage 3 - Local Group Cluster (~10 MLY)")
col_stage4 = bpy.data.collections.new("Stage 4 - Cosmic Web & Horizon (~93 GLY)")

scene.collection.children.link(col_stage1)
scene.collection.children.link(col_stage2)
scene.collection.children.link(col_stage3)
scene.collection.children.link(col_stage4)

# =========================================================================
# STAGE 1: SOLAR SYSTEM
# =========================================================================
mat_sun = create_pbr_material("Mat_Sun", "sun_photosphere.jpg", color=(1.0, 0.85, 0.4, 1.0), emission=2.5)
mat_earth = create_pbr_material("Mat_Earth", "earth_photosphere.jpg", color=(0.2, 0.5, 0.9, 1.0), roughness=0.3)
mat_jupiter = create_pbr_material("Mat_Jupiter", "jupiter_photosphere.jpg", color=(0.85, 0.7, 0.55, 1.0), roughness=0.7)
mat_neptune = create_pbr_material("Mat_Neptune", color=(0.2, 0.4, 0.95, 1.0), roughness=0.5)

# Sun
bpy.ops.mesh.primitive_uv_sphere_add(radius=3.5, segments=64, ring_count=32, location=(0, 0, 0))
sun_obj = bpy.context.active_object
sun_obj.name = "Sun_Photosphere"
col_stage1.objects.link(sun_obj)
bpy.context.collection.objects.unlink(sun_obj)
sun_obj.data.materials.append(mat_sun)

# Sun Dynamic Point Light
light_data = bpy.data.lights.new(name="Sun_Light", type='POINT')
light_data.energy = 5000.0
light_data.color = (1.0, 0.96, 0.88)
light_obj = bpy.data.objects.new("Sun_PointLight", light_data)
light_obj.location = (0, 0, 0)
col_stage1.objects.link(light_obj)

# Earth (1 AU)
create_orbit_ring("Earth_Orbit_1AU", 15.0, col_stage1, (0.22, 0.75, 1.0, 1.0))
bpy.ops.mesh.primitive_uv_sphere_add(radius=1.0, segments=48, ring_count=24, location=(15.0, 0, 0))
earth_obj = bpy.context.active_object
earth_obj.name = "Earth"
col_stage1.objects.link(earth_obj)
bpy.context.collection.objects.unlink(earth_obj)
earth_obj.data.materials.append(mat_earth)

# Jupiter (5.2 AU)
create_orbit_ring("Jupiter_Orbit_5AU", 32.0, col_stage1, (0.95, 0.75, 0.4, 1.0))
bpy.ops.mesh.primitive_uv_sphere_add(radius=2.0, segments=48, ring_count=24, location=(-22.6, 22.6, 0))
jup_obj = bpy.context.active_object
jup_obj.name = "Jupiter"
col_stage1.objects.link(jup_obj)
bpy.context.collection.objects.unlink(jup_obj)
jup_obj.data.materials.append(mat_jupiter)

# Neptune (30 AU - System Boundary)
create_orbit_ring("Neptune_Orbit_30AU", 55.0, col_stage1, (0.4, 0.6, 1.0, 1.0))
bpy.ops.mesh.primitive_uv_sphere_add(radius=1.5, segments=36, ring_count=18, location=(38.8, -38.8, 0))
nep_obj = bpy.context.active_object
nep_obj.name = "Neptune_Boundary"
col_stage1.objects.link(nep_obj)
bpy.context.collection.objects.unlink(nep_obj)
nep_obj.data.materials.append(mat_neptune)

# =========================================================================
# STAGE 2: MILKY WAY GALAXY
# =========================================================================
mat_mw = create_pbr_material("Mat_MilkyWay", "milky_way_disk.jpg", color=(0.9, 0.95, 1.0, 1.0), emission=1.8)
mat_sgra = create_pbr_material("Mat_SgrA", color=(1.0, 0.9, 0.5, 1.0), emission=4.0)
mat_beacon = create_pbr_material("Mat_SunBeacon", color=(1.0, 0.8, 0.1, 1.0), emission=5.0)

# Milky Way Disk
bpy.ops.mesh.primitive_plane_add(size=70.0, location=(0, 0, 0))
mw_obj = bpy.context.active_object
mw_obj.name = "Milky_Way_Disk"
col_stage2.objects.link(mw_obj)
bpy.context.collection.objects.unlink(mw_obj)
mw_obj.data.materials.append(mat_mw)

# Sagittarius A*
bpy.ops.mesh.primitive_uv_sphere_add(radius=1.2, location=(0, 0, 0))
sgra_obj = bpy.context.active_object
sgra_obj.name = "Sagittarius_A_Star"
col_stage2.objects.link(sgra_obj)
bpy.context.collection.objects.unlink(sgra_obj)
sgra_obj.data.materials.append(mat_sgra)

# Orion Spur Sun Beacon (8 kpc from core)
bpy.ops.mesh.primitive_uv_sphere_add(radius=0.7, location=(0, -18.0, 0.4))
beacon_obj = bpy.context.active_object
beacon_obj.name = "Orion_Spur_Sun_Beacon"
col_stage2.objects.link(beacon_obj)
bpy.context.collection.objects.unlink(beacon_obj)
beacon_obj.data.materials.append(mat_beacon)

# =========================================================================
# STAGE 3: LOCAL GROUP CLUSTER
# =========================================================================
mat_m31 = create_pbr_material("Mat_Andromeda", "andromeda_galaxy_disk.jpg", color=(0.85, 0.92, 1.0, 1.0), emission=1.8)
mat_m33 = create_pbr_material("Mat_Triangulum", color=(0.7, 0.85, 1.0, 1.0), emission=1.5)

# Milky Way in Local Group
bpy.ops.mesh.primitive_plane_add(size=22.0, location=(-18.0, 0, 0))
mw_lg = bpy.context.active_object
mw_lg.name = "LocalGroup_Milky_Way"
col_stage3.objects.link(mw_lg)
bpy.context.collection.objects.unlink(mw_lg)
mw_lg.data.materials.append(mat_mw)

# Andromeda M31 (2.5 MLY away)
bpy.ops.mesh.primitive_plane_add(size=30.0, location=(22.0, 8.0, 4.0))
m31_lg = bpy.context.active_object
m31_lg.name = "LocalGroup_Andromeda_M31"
m31_lg.rotation_euler = (math.radians(77.0), math.radians(35.0), 0)
col_stage3.objects.link(m31_lg)
bpy.context.collection.objects.unlink(m31_lg)
m31_lg.data.materials.append(mat_m31)

# Triangulum M33
bpy.ops.mesh.primitive_plane_add(size=12.0, location=(8.0, -20.0, -2.0))
m33_lg = bpy.context.active_object
m33_lg.name = "LocalGroup_Triangulum_M33"
col_stage3.objects.link(m33_lg)
bpy.context.collection.objects.unlink(m33_lg)
m33_lg.data.materials.append(mat_m33)

# =========================================================================
# STAGE 4: COSMIC WEB & CMB HORIZON (93 GLY)
# =========================================================================
mat_cmb = create_pbr_material("Mat_CMB_Horizon", "cmb_horizon_sky.jpg", color=(0.2, 0.6, 1.0, 1.0), emission=1.8)
mat_web = create_pbr_material("Mat_Cosmic_Web", "cosmic_web_simulation.jpg", color=(0.95, 0.8, 0.3, 1.0), emission=2.2)

# CMB Horizon Inverted Sphere
bpy.ops.mesh.primitive_uv_sphere_add(radius=100.0, segments=64, ring_count=48, location=(0, 0, 0))
cmb_obj = bpy.context.active_object
cmb_obj.name = "Observable_Universe_CMB_Horizon"
col_stage4.objects.link(cmb_obj)
bpy.context.collection.objects.unlink(cmb_obj)
cmb_obj.data.materials.append(mat_cmb)

# Inner Cosmic Web Plane
bpy.ops.mesh.primitive_plane_add(size=120.0, location=(0, 0, 0))
web_obj = bpy.context.active_object
web_obj.name = "Cosmic_Web_Filaments"
col_stage4.objects.link(web_obj)
bpy.context.collection.objects.unlink(web_obj)
web_obj.data.materials.append(mat_web)

# =========================================================================
# CINEMATIC CAMERAS
# =========================================================================
col_cams = bpy.data.collections.new("Cameras")
scene.collection.children.link(col_cams)

def create_camera(name, location, rotation, fov=45.0):
    cam_data = bpy.data.cameras.new(name)
    cam_data.lens_unit = 'FOV'
    cam_data.angle = math.radians(fov)
    cam_data.clip_start = 0.5
    cam_data.clip_end = 10000.0
    cam_obj = bpy.data.objects.new(name, cam_data)
    cam_obj.location = location
    cam_obj.rotation_euler = rotation
    col_cams.objects.link(cam_obj)
    return cam_obj

# Stage 1 Oblique View
cam_s1 = create_camera("Camera_Stage1_SolarSystem", (0, -85.0, 50.0), (math.radians(60.0), 0, 0))
# Stage 2 Top-Down Galaxy View
cam_s2 = create_camera("Camera_Stage2_MilkyWay", (0, -60.0, 95.0), (math.radians(58.0), 0, 0))
# Stage 3 Local Group View
cam_s3 = create_camera("Camera_Stage3_LocalGroup", (0, -90.0, 80.0), (math.radians(50.0), 0, 0))
# Stage 4 Cosmic Horizon View
cam_s4 = create_camera("Camera_Stage4_CosmicWeb", (0, -180.0, 110.0), (math.radians(58.0), 0, 0))

# Set active camera
scene.camera = cam_s1

# Save unified .blend file
bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUTPUT)
print(f"\n[Blender 5.2 Pipeline] SUCCESS! Unified project saved to:\n  {BLEND_OUTPUT}")
