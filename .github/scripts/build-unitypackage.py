"""Wrap the VPM ZIP in an offline Unity editor installer."""

import argparse
import io
import json
from pathlib import Path
import tarfile
import uuid
import zipfile


INSTALL_ROOT = "Assets/StrangeToolkitInstaller"
PAYLOAD_GUID = "1c564ac52f754aee99aa55afbfb57e17"


def build(package_zip: Path, output: Path) -> None:
    with zipfile.ZipFile(package_zip) as package:
        manifest = json.loads(package.read("package.json"))
        if manifest["name"] != "com.bellastrangevr.strangetoolkit":
            raise ValueError("The ZIP does not contain Strange Toolkit")

    installer = Path(__file__).resolve().parents[1] / "installer"
    files = {
        "Editor/StrangeToolkitInstaller.cs": (installer / "StrangeToolkitInstaller.cs").read_bytes(),
        "Editor/StrangeToolkit.Installer.asmdef": json.dumps({
            "name": "StrangeToolkit.Installer",
            "includePlatforms": ["Editor"],
            "autoReferenced": False,
        }, indent=2).encode(),
        "StrangeToolkit.zip.bytes": package_zip.read_bytes(),
    }
    entries = {"": None, "Editor": None, **files}
    with tarfile.open(output, "w:gz", format=tarfile.USTAR_FORMAT) as archive:
        for relative, data in entries.items():
            path = f"{INSTALL_ROOT}/{relative}".rstrip("/")
            guid = PAYLOAD_GUID if relative == "StrangeToolkit.zip.bytes" else uuid.uuid5(uuid.NAMESPACE_URL, path).hex
            folder = "folderAsset: yes\n" if data is None else ""
            metadata = f"fileFormatVersion: 2\nguid: {guid}\n{folder}"
            members = {"asset.meta": metadata.encode(), "pathname": path.encode()}
            if data is not None:
                members["asset"] = data
            for name, content in members.items():
                member = tarfile.TarInfo(f"{guid}/{name}")
                member.size = len(content)
                member.mode = 0o644
                archive.addfile(member, io.BytesIO(content))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_zip", type=Path)
    parser.add_argument("output", type=Path)
    arguments = parser.parse_args()
    build(arguments.package_zip, arguments.output)
