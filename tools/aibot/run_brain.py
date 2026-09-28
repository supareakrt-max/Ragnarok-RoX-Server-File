#!/usr/bin/env python3
"""Start the AI brain.

    python run_brain.py -c config.json
"""

import argparse
import asyncio
import json
import logging
import os
import sys

from aibot.brain import Brain


def main():
    parser = argparse.ArgumentParser(description="rAthena AI bot brain")
    parser.add_argument("-c", "--config", default="config.json")
    parser.add_argument("-v", "--verbose", action="store_true")
    args = parser.parse_args()

    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s %(levelname)-7s %(name)s: %(message)s",
    )

    if not os.path.exists(args.config):
        sys.exit("config %s not found (copy config.example.json)" % args.config)
    with open(args.config, "rb") as fp:
        raw = fp.read()
    try:
        text = raw.decode("utf-8-sig")
    except UnicodeDecodeError:
        text = raw.decode("cp874")  # saved as Thai ANSI by Notepad
    config = json.loads(text)

    brain = Brain(config, base_dir=os.path.dirname(os.path.abspath(args.config)))
    try:
        asyncio.run(brain.run())
    except KeyboardInterrupt:
        pass
    finally:
        brain.save_memories()


if __name__ == "__main__":
    main()
