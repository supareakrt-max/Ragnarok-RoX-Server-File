#!/usr/bin/env python3
"""Keep config.json's "bots" list in sync with the bot characters.

Used by RO_Manager, but works on its own too:

    python manage.py sync --sql bots.sql [--json bots.json]   add the characters of a bots.sql
    python manage.py remove --names KingIce,PWin              drop bots from config.json
    python manage.py names                                    print the configured bot names

Prints one JSON line with the result so callers can parse it.
"""

import argparse
import json
import os
import random
import re
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
CONFIG = os.path.join(HERE, "config.json")
EXAMPLE = os.path.join(HERE, "config.example.json")
PERSONALITIES = ["hardcore", "merchant", "chill"]
CHAR_INSERT = re.compile(r"INSERT INTO `char` \([^)]*\) VALUES \((\d+),(\d+),\d+,'((?:[^'\\]|\\.)*)'", re.I)


def read_json(path):
    """Read JSON saved as UTF-8 (with or without BOM) or as Thai ANSI (cp874),
    which is what Notepad produces on Thai Windows."""
    with open(path, "rb") as fp:
        raw = fp.read()
    for enc in ("utf-8-sig", "cp874"):
        try:
            return json.loads(raw.decode(enc))
        except UnicodeDecodeError:
            continue
    return json.loads(raw.decode("utf-8", "replace"))


def load_config():
    if not os.path.exists(CONFIG):
        shutil.copyfile(EXAMPLE, CONFIG)
        cfg = read_json(CONFIG)
        cfg["bots"] = []  # drop the example names
        return cfg
    return read_json(CONFIG)


def save_config(cfg):
    tmp = CONFIG + ".tmp"
    with open(tmp, "w", encoding="utf-8") as fp:
        json.dump(cfg, fp, ensure_ascii=False, indent=2)
    os.replace(tmp, CONFIG)


def names_in_sql(path):
    with open(path, encoding="utf-8", errors="replace") as fp:
        text = fp.read()
    return [re.sub(r"\\(.)", r"\1", m.group(3)) for m in CHAR_INSERT.finditer(text)]


def cmd_sync(args):
    cfg = load_config()
    bots = cfg.setdefault("bots", [])
    have = {b["name"].lower() for b in bots}
    personality = {}
    json_path = args.json or os.path.splitext(args.sql)[0] + ".json"
    if os.path.exists(json_path):
        try:
            for b in read_json(json_path).get("bots", []):
                personality[b["name"].lower()] = b.get("personality")
        except (ValueError, KeyError):
            pass
    added = []
    for name in names_in_sql(args.sql):
        if name.lower() in have:
            continue
        p = personality.get(name.lower()) or random.choice(PERSONALITIES)
        bots.append({"name": name, "personality": p})
        have.add(name.lower())
        added.append(name)
    save_config(cfg)
    return {"ok": True, "added": added, "total": len(bots)}


def cmd_remove(args):
    cfg = load_config()
    drop = {n.strip().lower() for n in args.names.split(",") if n.strip()}
    before = len(cfg.get("bots", []))
    cfg["bots"] = [b for b in cfg.get("bots", []) if b["name"].lower() not in drop]
    save_config(cfg)
    return {"ok": True, "removed": before - len(cfg["bots"]), "total": len(cfg["bots"])}


def cmd_names(args):
    cfg = load_config()
    return {"ok": True, "names": [b["name"] for b in cfg.get("bots", [])]}


def main():
    ap = argparse.ArgumentParser()
    sub = ap.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("sync")
    s.add_argument("--sql", required=True)
    s.add_argument("--json")
    r = sub.add_parser("remove")
    r.add_argument("--names", required=True)
    sub.add_parser("names")
    args = ap.parse_args()
    try:
        result = {"sync": cmd_sync, "remove": cmd_remove, "names": cmd_names}[args.cmd](args)
    except Exception as exc:  # report errors as JSON for RO_Manager
        result = {"ok": False, "error": str(exc)}
    sys.stdout.write(json.dumps(result, ensure_ascii=False) + "\n")
    return 0 if result.get("ok") else 1


if __name__ == "__main__":
    sys.exit(main())
