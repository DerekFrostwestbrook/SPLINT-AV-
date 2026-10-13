import argparse
import gzip
import re
import sys
from pathlib import Path


HASH_LINE = re.compile(r"^[0-9a-fA-F]{64}$")
RECORD_SIZE = 32
CHUNK_SIZE = 1024 * 1024


def pack_hashes(source, destination, compress=True):
    source = Path(source)
    destination = Path(destination)
    records = []
    with source.open("r", encoding="ascii", errors="ignore") as hashes:
        for line in hashes:
            value = line.strip()
            if HASH_LINE.fullmatch(value):
                records.append(bytes.fromhex(value))

    if not records:
        raise ValueError("No valid SHA-256 hashes found in " + str(source))

    records.sort()
    written = 0
    chunk = bytearray()
    previous = None
    with destination.open("wb") as target:
        output = gzip.GzipFile(fileobj=target, mode="wb", compresslevel=1, mtime=0) if compress else target
        try:
            for digest in records:
                if digest == previous:
                    continue
                chunk.extend(digest)
                previous = digest
                written += 1
                if len(chunk) >= CHUNK_SIZE:
                    output.write(chunk)
                    chunk.clear()
            if chunk:
                output.write(chunk)
        finally:
            if compress:
                output.close()

    return written


def main():
    parser = argparse.ArgumentParser(description="Pack a SHA-256 text list as sorted binary records.")
    parser.add_argument("source")
    parser.add_argument("destination")
    parser.add_argument("--binary", action="store_true", help="write raw 32-byte digest records instead of gzip")
    args = parser.parse_args()
    try:
        count = pack_hashes(args.source, args.destination, compress=not args.binary)
    except (OSError, ValueError) as error:
        print("Hash packing failed: " + str(error), file=sys.stderr)
        return 1
    print("Packed {:,} unique SHA-256 hashes.".format(count))
    return 0


if __name__ == "__main__":
    sys.exit(main())
