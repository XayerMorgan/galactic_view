"""
Blender 5.2 Automation Pipeline for Project GALAXY
Procedurally models and exports high-detail 3D Sci-Fi Exploration Starships:
1. Deep-Space Survey Flagship (Sleek angular wedge hull, forward sensor prow, dual radiator fins, glowing cyan ion drives)
2. Sol Scout Interceptor (Angular delta lifting body, twin ion exhaust cones, golden canopy)
Zero Star Trek saucer geometry, zero default cubes/cameras/lights.
"""

import bpy
import math
import os
import sys

BASE_DIR = os.path.abspath(r"d:\Vibe Code Repo\galactic_view\unity_cosmic_engine\Assets\GALAXY\Models_3D")
os.makedirs(BASE_DIR, exist_ok=True)

def clear_scene():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m, do_unlink=True)
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m, do_unlink=True)
    for c in list(bpy.data.cameras):
        bpy.data.cameras.remove(c, do_unlink=True)
    for l in list(bpy.data.lights):
        bpy.data.lights.remove(l, do_unlink=True)

def create_emissive_material(name, color, emission_strength=6.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    
    node_out = nodes.new(type='ShaderNodeOutputMaterial')
    node_emit = nodes.new(type='ShaderNodeEmission')
    node_emit.inputs['Color'].default_value = color
    node_emit.inputs['Strength'].default_value = emission_strength
    
    mat.node_tree.links.new(node_emit.outputs['Emission'], node_out.inputs['Surface'])
    return mat

def create_hull_material(name, base_color=(0.20, 0.24, 0.30, 1.0), roughness=0.3, metallic=0.85):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    
    node_out = nodes.new(type='ShaderNodeOutputMaterial')
    bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    bsdf.inputs['Base Color'].default_value = base_color
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    
    mat.node_tree.links.new(bsdf.outputs['BSDF'], node_out.inputs['Surface'])
    return mat

def build_survey_flagship():
    clear_scene()
    
    mat_hull = create_hull_material("Mat_FlagshipHull", (0.28, 0.32, 0.38, 1.0), roughness=0.3, metallic=0.75)
    mat_dark_armor = create_hull_material("Mat_DarkArmor", (0.09, 0.12, 0.16, 1.0), roughness=0.4, metallic=0.9)
    mat_ion_glow = create_emissive_material("Mat_IonCyan", (0.0, 0.9, 1.0, 1.0), emission_strength=7.0)
    mat_sensor_glow = create_emissive_material("Mat_SensorAmber", (1.0, 0.7, 0.1, 1.0), emission_strength=6.0)

    # 1. Main Angular Hull (Deep-space wedge prow)
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=4.5, radius2=1.2, depth=22.0, location=(0, 0, 0))
    hull = bpy.context.active_object
    hull.name = "Flagship_Main_Hull"
    hull.rotation_euler = (math.radians(90), 0, 0)
    hull.scale = (1.1, 0.35, 1.0)
    hull.data.materials.append(mat_hull)

    # 2. Forward Sensor Array Dome
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.3, location=(0, 11.2, 0))
    sensor = bpy.context.active_object
    sensor.name = "Flagship_Sensor_Prow"
    sensor.scale = (0.9, 1.4, 0.5)
    sensor.data.materials.append(mat_sensor_glow)

    # 3. Command Deck Island
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.5, 1.2))
    bridge = bpy.context.active_object
    bridge.name = "Flagship_Command_Bridge"
    bridge.scale = (2.2, 7.0, 0.9)
    bridge.data.materials.append(mat_dark_armor)

    # 4. Radiator / Solar Array Wings
    for side, x_sign in [("Port", -1), ("Starboard", 1)]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x_sign * 5.2, -3.0, 0.2))
        wing = bpy.context.active_object
        wing.name = f"Flagship_Radiator_{side}"
        wing.scale = (5.0, 10.0, 0.18)
        wing.rotation_euler = (0, math.radians(x_sign * 12), math.radians(x_sign * 8))
        wing.data.materials.append(mat_hull)

        # Wingtip Sensor Spikes
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.18, depth=7.0, location=(x_sign * 7.8, -3.0, 0.5))
        spike = bpy.context.active_object
        spike.name = f"Flagship_Spike_{side}"
        spike.rotation_euler = (math.radians(90), 0, 0)
        spike.data.materials.append(mat_dark_armor)

    # 5. Dual Heavy Fusion / Ion Engine Nozzles (Aft)
    for side, x_sign in [("Port", -1), ("Starboard", 1)]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=1.3, depth=4.5, location=(x_sign * 2.2, -12.5, 0))
        engine_nozzle = bpy.context.active_object
        engine_nozzle.name = f"Flagship_Engine_Nozzle_{side}"
        engine_nozzle.rotation_euler = (math.radians(90), 0, 0)
        engine_nozzle.data.materials.append(mat_dark_armor)

        # Glowing Ion Thruster Core
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.95, depth=0.8, location=(x_sign * 2.2, -14.2, 0))
        exhaust = bpy.context.active_object
        exhaust.name = f"Flagship_Ion_Glow_{side}"
        exhaust.rotation_euler = (math.radians(90), 0, 0)
        exhaust.data.materials.append(mat_ion_glow)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.shade_smooth()

    fbx_out = os.path.join(BASE_DIR, "USS_Astronautica_Flagship.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx_out, use_selection=False)
    print("Exported Survey Flagship:", fbx_out)

def build_sol_scout_ship():
    clear_scene()

    mat_stealth_hull = create_hull_material("Mat_StealthHull", (0.09, 0.11, 0.15, 1.0), roughness=0.3, metallic=0.95)
    mat_canopy = create_hull_material("Mat_GoldCanopy", (0.95, 0.75, 0.20, 1.0), roughness=0.1, metallic=0.9)
    mat_ion_exhaust = create_emissive_material("Mat_IonCyan", (0.0, 0.85, 1.0, 1.0), emission_strength=7.0)

    # 1. Lifting-Body Fuselage (Dart delta shape)
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=4.0, radius2=0.4, depth=12.0, location=(0, 0, 0))
    hull = bpy.context.active_object
    hull.name = "Scout_Fuselage"
    hull.rotation_euler = (math.radians(90), 0, 0)
    hull.scale = (1.2, 0.4, 1.0)
    hull.data.materials.append(mat_stealth_hull)

    # 2. Angular Wings with Cant
    for side, x_sign in [("Port", -1), ("Starboard", 1)]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x_sign * 3.8, -2.0, 0.1))
        wing = bpy.context.active_object
        wing.name = f"Scout_Wing_{side}"
        wing.scale = (4.0, 7.0, 0.15)
        wing.rotation_euler = (0, math.radians(x_sign * 15), math.radians(x_sign * 10))
        wing.data.materials.append(mat_stealth_hull)

        # Canted Twin Vertical Stabilizers
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x_sign * 3.2, -4.5, 1.4))
        fin = bpy.context.active_object
        fin.name = f"Scout_Fin_{side}"
        fin.scale = (0.15, 3.0, 2.2)
        fin.rotation_euler = (math.radians(15), math.radians(x_sign * -22), 0)
        fin.data.materials.append(mat_stealth_hull)

    # 3. Cockpit Canopy Glass (Sleek aerodynamic teardrop)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, radius=1.0, location=(0, 2.0, 0.6))
    canopy = bpy.context.active_object
    canopy.name = "Scout_Cockpit_Canopy"
    canopy.scale = (0.75, 2.4, 0.45)
    canopy.data.materials.append(mat_canopy)

    # 4. Twin Ion Thruster Nozzles
    for side, x_sign in [("Port", -1), ("Starboard", 1)]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.65, depth=1.8, location=(x_sign * 1.3, -6.5, 0))
        thruster = bpy.context.active_object
        thruster.name = f"Scout_Thruster_{side}"
        thruster.rotation_euler = (math.radians(90), 0, 0)
        thruster.data.materials.append(mat_stealth_hull)

        # Glowing exhaust disk
        bpy.ops.mesh.primitive_circle_add(vertices=20, radius=0.55, location=(x_sign * 1.3, -7.42, 0))
        exhaust = bpy.context.active_object
        exhaust.name = f"Scout_Exhaust_{side}"
        exhaust.rotation_euler = (math.radians(90), 0, 0)
        exhaust.data.materials.append(mat_ion_exhaust)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.shade_smooth()

    fbx_out = os.path.join(BASE_DIR, "Sol_Scout_Ship.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx_out, use_selection=False)
    print("Exported Sol Scout Ship:", fbx_out)

if __name__ == "__main__":
    print("Building Project GALAXY 3D Starships...")
    build_survey_flagship()
    build_sol_scout_ship()
    print("All Starships built successfully!")
