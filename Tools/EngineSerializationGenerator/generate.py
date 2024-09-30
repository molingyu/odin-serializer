#!/usr/bin/env python3
"""
Generates EngineSerializationData.g.cs from Godot's GDExtension API dump.

The C# bindings of Godot (GodotSharp) are generated from the same API information,
so this produces property lists that match the engine's own ClassDB metadata for all
internal (engine-defined) Node and Resource classes.

Obtaining extension_api.json (three sources, pick one):
  - Local file:            pass it via --input (or positionally)
  - Local Godot editor:    pass --godot <editor exe>; it is run headless with
                           --dump-extension-api and the dump is used as input
  - Download:              pass --download <ref> to fetch the dump bundled with
                           godot-cpp (ref = tag or branch, e.g. godot-4.5-stable,
                           4.5, master). Note: extension_api.json is NOT in the
                           godot repo itself (core/extension/gdextension_interface.json
                           is a different file — the C ABI schema, unusable here).

Usage:
  python generate.py [input.json] [output.cs]
  python generate.py --input extension_api_4.6.json --output out.cs
  python generate.py --godot "D:/Godot/Godot_v4.6-stable_mono_win64.exe"
  python generate.py --download godot-4.5-stable --keep-json extension_api_4.5.json

Defaults:
  input : extension_api_4.6.json next to this script
  output: ../../OdinSerializer/Engine Integration/Godot/Generated/EngineSerializationData.g.cs
"""

import argparse
import json
import os
import subprocess
import sys
import tempfile
import urllib.request

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DEFAULT_INPUT = os.path.join(SCRIPT_DIR, "extension_api_4.6.json")
DEFAULT_OUTPUT = os.path.normpath(
    os.path.join(SCRIPT_DIR, "..", "..", "OdinSerializer", "Engine Integration", "Godot", "Generated", "EngineSerializationData.g.cs")
)
GODOT_CPP_API_URL = "https://raw.githubusercontent.com/godotengine/godot-cpp/{ref}/gdextension/extension_api.json"

# Properties that must never be serialized: bookkeeping handled elsewhere or
# dangerous to assign during deserialization.
DENYLIST = {"resource_path", "script"}

# Root classes whose descendants get serialization data.
SUPPORTED_ROOTS = ("Node", "Resource")

# api_type values to include ("editor" classes do not exist in export builds).
SUPPORTED_API_TYPES = ("core", "editor_extension")


def load_classes(api):
    classes = {}
    for cls in api["classes"]:
        classes[cls["name"]] = cls
    return classes


def get_inheritance_chain(classes, name):
    """Returns the chain from the given class up to (and excluding) the root-most class."""
    chain = []
    current = name
    while current:
        cls = classes.get(current)
        if cls is None:
            break
        chain.append(cls)
        current = cls.get("inherits", "")
    return chain


def flatten_properties(chain):
    """Flattens properties of the inheritance chain, derived class wins. Keeps only
    properties that have both a setter and a getter."""
    props = {}
    for cls in reversed(chain):  # root first
        for prop in cls.get("properties", []):
            if not prop.get("setter") or not prop.get("getter"):
                continue
            name = prop["name"]
            if name in DENYLIST:
                continue
            props[name] = None  # dict keeps insertion order in py3.7+
    return list(props.keys())


def dump_from_editor(godot_exe, work_dir):
    """Runs the Godot editor headless to produce extension_api.json in work_dir."""
    print(f"Dumping extension API with: {godot_exe}")
    subprocess.run([godot_exe, "--headless", "--dump-extension-api"], cwd=work_dir, check=True)
    dumped = os.path.join(work_dir, "extension_api.json")
    if not os.path.isfile(dumped):
        sys.exit(f"error: --dump-extension-api did not produce {dumped}")
    return dumped


def download_from_godot_cpp(ref, work_dir):
    """Downloads the extension_api.json bundled with godot-cpp for the given ref."""
    url = GODOT_CPP_API_URL.format(ref=ref)
    print(f"Downloading {url}")
    target = os.path.join(work_dir, "extension_api.json")
    try:
        urllib.request.urlretrieve(url, target)
    except Exception as ex:
        sys.exit(f"error: failed to download {url}: {ex}")
    return target


def read_api_version(api):
    header = api.get("header", {})
    return f"{header.get('version_major', '?')}.{header.get('version_minor', '?')}.{header.get('version_patch', '?')}"


def generate(api_path, out_path):
    with open(api_path, encoding="utf-8") as f:
        api = json.load(f)

    if "classes" not in api:
        sys.exit(f"error: {api_path} does not look like an extension_api.json (no 'classes' key)")

    version = read_api_version(api)
    classes = load_classes(api)
    entries = []

    for name, cls in classes.items():
        if not cls.get("is_instantiable", False):
            continue
        if cls.get("api_type", "core") not in SUPPORTED_API_TYPES:
            continue

        chain = get_inheritance_chain(classes, name)
        chain_names = {c["name"] for c in chain}

        if not any(root in chain_names for root in SUPPORTED_ROOTS):
            continue

        props = flatten_properties(chain)
        entries.append((name, props))

    entries.sort(key=lambda e: e[0])

    os.makedirs(os.path.dirname(out_path), exist_ok=True)

    with open(out_path, "w", encoding="utf-8", newline="\n") as f:
        f.write("// <auto-generated/>\n")
        f.write("// Generated by Tools/EngineSerializationGenerator/generate.py from\n")
        f.write(f"// {os.path.basename(api_path)} (Godot {version} extension API). Do not edit manually.\n")
        f.write("// Regenerate with: python generate.py --input <extension_api.json>\n\n")
        f.write("namespace OdinSerializer\n")
        f.write("{\n")
        f.write("    using System.Collections.Generic;\n\n")
        f.write("    public static partial class EngineSerializationData\n")
        f.write("    {\n")
        f.write("        static partial void RegisterGeneratedTypes(Dictionary<string, string[]> map)\n")
        f.write("        {\n")
        for name, props in entries:
            if props:
                joined = "\", \"".join(props)
                f.write(f"            map[\"{name}\"] = new string[] {{ \"{joined}\" }};\n")
            else:
                f.write(f"            map[\"{name}\"] = System.Array.Empty<string>();\n")
        f.write("        }\n")
        f.write("    }\n")
        f.write("}\n")

    total_props = sum(len(p) for _, p in entries)
    print(f"Godot {version}: wrote {len(entries)} engine classes ({total_props} properties) to {out_path}")


def main():
    parser = argparse.ArgumentParser(
        description="Generates EngineSerializationData.g.cs from Godot's GDExtension API dump.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("input", nargs="?", help="Path to extension_api.json (default: extension_api_4.6.json next to this script)")
    parser.add_argument("output", nargs="?", help=f"Output .cs path (default: {DEFAULT_OUTPUT})")
    parser.add_argument("-i", "--input-file", dest="input_file", help="Path to extension_api.json (same as the positional input)")
    parser.add_argument("-o", "--output-file", dest="output_file", help="Output .cs path (same as the positional output)")
    parser.add_argument("--godot", metavar="EXE", help="Path to a Godot editor executable; dump the API with --dump-extension-api first")
    parser.add_argument("--download", metavar="REF", help="Download the API dump bundled with godot-cpp (tag or branch, e.g. godot-4.5-stable, 4.5, master)")
    parser.add_argument("--keep-json", metavar="PATH", help="Also save the obtained extension_api.json to this path")
    args = parser.parse_args()

    out_path = args.output_file or args.output or DEFAULT_OUTPUT
    api_path = None

    with tempfile.TemporaryDirectory() as work_dir:
        if args.godot:
            api_path = dump_from_editor(args.godot, work_dir)
        elif args.download:
            api_path = download_from_godot_cpp(args.download, work_dir)
        else:
            api_path = args.input_file or args.input or DEFAULT_INPUT

        # A dump/download lives in the temp dir; persist the input JSON if asked
        # (or always when it came from an external source).
        if os.path.dirname(os.path.abspath(api_path)) == os.path.abspath(work_dir):
            with open(api_path, encoding="utf-8") as f:
                version = read_api_version(json.load(f))
            keep = args.keep_json or os.path.join(SCRIPT_DIR, f"extension_api_{version}.json")
            with open(api_path, encoding="utf-8") as src, open(keep, "w", encoding="utf-8", newline="\n") as dst:
                dst.write(src.read())
            print(f"Saved API dump to {keep}")
            api_path = keep
        elif args.keep_json:
            with open(api_path, encoding="utf-8") as src, open(args.keep_json, "w", encoding="utf-8", newline="\n") as dst:
                dst.write(src.read())
            print(f"Saved API dump to {args.keep_json}")

        generate(api_path, out_path)


if __name__ == "__main__":
    main()
