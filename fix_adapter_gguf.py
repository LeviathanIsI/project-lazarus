#!/usr/bin/env python3
"""
Fix GGUF adapter file by adding missing 'general.type = adapter' metadata.
This script modifies GGUF files to be compatible with llama.cpp's adapter requirements.
"""

import struct
import sys
import os
from pathlib import Path
from typing import Any, Dict, List, Tuple
import shutil

class GGUFReader:
    """Read and parse GGUF file format."""

    GGUF_MAGIC = 0x46554747  # "GGUF" in little-endian
    GGUF_VERSION = 3

    GGUF_TYPE_UINT8   = 0
    GGUF_TYPE_INT8    = 1
    GGUF_TYPE_UINT16  = 2
    GGUF_TYPE_INT16   = 3
    GGUF_TYPE_UINT32  = 4
    GGUF_TYPE_INT32   = 5
    GGUF_TYPE_FLOAT32 = 6
    GGUF_TYPE_BOOL    = 7
    GGUF_TYPE_STRING  = 8
    GGUF_TYPE_ARRAY   = 9
    GGUF_TYPE_UINT64  = 10
    GGUF_TYPE_INT64   = 11
    GGUF_TYPE_FLOAT64 = 12

    def __init__(self, file_path: Path):
        self.file_path = file_path
        self.metadata: Dict[str, Any] = {}
        self.tensor_info: List[Dict] = []

    def read(self):
        """Read GGUF file and parse metadata."""
        with open(self.file_path, 'rb') as f:
            # Read header
            magic = struct.unpack('<I', f.read(4))[0]
            if magic != self.GGUF_MAGIC:
                raise ValueError(f"Invalid GGUF magic: {magic:08x}")

            version = struct.unpack('<I', f.read(4))[0]
            if version != self.GGUF_VERSION:
                raise ValueError(f"Unsupported GGUF version: {version}")

            tensor_count = struct.unpack('<Q', f.read(8))[0]
            metadata_kv_count = struct.unpack('<Q', f.read(8))[0]

            print(f"GGUF version: {version}")
            print(f"Tensor count: {tensor_count}")
            print(f"Metadata KV count: {metadata_kv_count}")

            # Read metadata key-value pairs
            for _ in range(metadata_kv_count):
                key_len = struct.unpack('<Q', f.read(8))[0]
                key = f.read(key_len).decode('utf-8')
                value_type = struct.unpack('<I', f.read(4))[0]
                value = self._read_value(f, value_type)
                self.metadata[key] = (value_type, value)
                print(f"  {key}: {value}")

            self.header_end = f.tell()

            # Read tensor info
            for _ in range(tensor_count):
                name_len = struct.unpack('<Q', f.read(8))[0]
                name = f.read(name_len).decode('utf-8')
                n_dims = struct.unpack('<I', f.read(4))[0]
                dims = [struct.unpack('<Q', f.read(8))[0] for _ in range(n_dims)]
                dtype = struct.unpack('<I', f.read(4))[0]
                offset = struct.unpack('<Q', f.read(8))[0]

                self.tensor_info.append({
                    'name': name,
                    'dims': dims,
                    'dtype': dtype,
                    'offset': offset
                })

            self.tensor_data_start = f.tell()

            # Align to 32 bytes for tensor data
            alignment = 32
            pad = (alignment - (self.tensor_data_start % alignment)) % alignment
            self.tensor_data_start += pad

    def _read_value(self, f, value_type: int) -> Any:
        """Read a value based on its type."""
        if value_type == self.GGUF_TYPE_UINT8:
            return struct.unpack('B', f.read(1))[0]
        elif value_type == self.GGUF_TYPE_INT8:
            return struct.unpack('b', f.read(1))[0]
        elif value_type == self.GGUF_TYPE_UINT16:
            return struct.unpack('<H', f.read(2))[0]
        elif value_type == self.GGUF_TYPE_INT16:
            return struct.unpack('<h', f.read(2))[0]
        elif value_type == self.GGUF_TYPE_UINT32:
            return struct.unpack('<I', f.read(4))[0]
        elif value_type == self.GGUF_TYPE_INT32:
            return struct.unpack('<i', f.read(4))[0]
        elif value_type == self.GGUF_TYPE_FLOAT32:
            return struct.unpack('<f', f.read(4))[0]
        elif value_type == self.GGUF_TYPE_BOOL:
            return struct.unpack('?', f.read(1))[0]
        elif value_type == self.GGUF_TYPE_STRING:
            str_len = struct.unpack('<Q', f.read(8))[0]
            return f.read(str_len).decode('utf-8')
        elif value_type == self.GGUF_TYPE_ARRAY:
            array_type = struct.unpack('<I', f.read(4))[0]
            array_len = struct.unpack('<Q', f.read(8))[0]
            return [self._read_value(f, array_type) for _ in range(array_len)]
        elif value_type == self.GGUF_TYPE_UINT64:
            return struct.unpack('<Q', f.read(8))[0]
        elif value_type == self.GGUF_TYPE_INT64:
            return struct.unpack('<q', f.read(8))[0]
        elif value_type == self.GGUF_TYPE_FLOAT64:
            return struct.unpack('<d', f.read(8))[0]
        else:
            raise ValueError(f"Unknown value type: {value_type}")


class GGUFWriter:
    """Write GGUF file with modified metadata."""

    def __init__(self, reader: GGUFReader):
        self.reader = reader

    def write(self, output_path: Path):
        """Write modified GGUF file with added metadata."""
        # Check if general.type already exists
        has_general_type = 'general.type' in self.reader.metadata

        if has_general_type:
            print(f"\nNote: 'general.type' already exists with value: {self.reader.metadata['general.type'][1]}")
            print("Updating to 'adapter'...")
        else:
            print("\nAdding 'general.type = adapter' metadata...")

        # Add or update the general.type metadata
        self.reader.metadata['general.type'] = (GGUFReader.GGUF_TYPE_STRING, 'adapter')

        with open(self.reader.file_path, 'rb') as src:
            with open(output_path, 'wb') as dst:
                # Write header
                dst.write(struct.pack('<I', GGUFReader.GGUF_MAGIC))
                dst.write(struct.pack('<I', GGUFReader.GGUF_VERSION))
                dst.write(struct.pack('<Q', len(self.reader.tensor_info)))
                dst.write(struct.pack('<Q', len(self.reader.metadata)))

                # Write metadata
                for key, (value_type, value) in self.reader.metadata.items():
                    key_bytes = key.encode('utf-8')
                    dst.write(struct.pack('<Q', len(key_bytes)))
                    dst.write(key_bytes)
                    dst.write(struct.pack('<I', value_type))
                    self._write_value(dst, value_type, value)

                # Write tensor info
                for tensor in self.reader.tensor_info:
                    name_bytes = tensor['name'].encode('utf-8')
                    dst.write(struct.pack('<Q', len(name_bytes)))
                    dst.write(name_bytes)
                    dst.write(struct.pack('<I', len(tensor['dims'])))
                    for dim in tensor['dims']:
                        dst.write(struct.pack('<Q', dim))
                    dst.write(struct.pack('<I', tensor['dtype']))
                    dst.write(struct.pack('<Q', tensor['offset']))

                # Align to 32 bytes for tensor data
                current_pos = dst.tell()
                alignment = 32
                pad = (alignment - (current_pos % alignment)) % alignment
                dst.write(b'\x00' * pad)

                # Copy tensor data
                src.seek(self.reader.tensor_data_start)
                shutil.copyfileobj(src, dst)

    def _write_value(self, f, value_type: int, value: Any):
        """Write a value based on its type."""
        if value_type == GGUFReader.GGUF_TYPE_UINT8:
            f.write(struct.pack('B', value))
        elif value_type == GGUFReader.GGUF_TYPE_INT8:
            f.write(struct.pack('b', value))
        elif value_type == GGUFReader.GGUF_TYPE_UINT16:
            f.write(struct.pack('<H', value))
        elif value_type == GGUFReader.GGUF_TYPE_INT16:
            f.write(struct.pack('<h', value))
        elif value_type == GGUFReader.GGUF_TYPE_UINT32:
            f.write(struct.pack('<I', value))
        elif value_type == GGUFReader.GGUF_TYPE_INT32:
            f.write(struct.pack('<i', value))
        elif value_type == GGUFReader.GGUF_TYPE_FLOAT32:
            f.write(struct.pack('<f', value))
        elif value_type == GGUFReader.GGUF_TYPE_BOOL:
            f.write(struct.pack('?', value))
        elif value_type == GGUFReader.GGUF_TYPE_STRING:
            str_bytes = value.encode('utf-8')
            f.write(struct.pack('<Q', len(str_bytes)))
            f.write(str_bytes)
        elif value_type == GGUFReader.GGUF_TYPE_ARRAY:
            # For simplicity, assuming homogeneous arrays
            if len(value) > 0:
                # Infer array type from first element
                first = value[0]
                if isinstance(first, bool):
                    array_type = GGUFReader.GGUF_TYPE_BOOL
                elif isinstance(first, int):
                    array_type = GGUFReader.GGUF_TYPE_INT32
                elif isinstance(first, float):
                    array_type = GGUFReader.GGUF_TYPE_FLOAT32
                elif isinstance(first, str):
                    array_type = GGUFReader.GGUF_TYPE_STRING
                else:
                    array_type = GGUFReader.GGUF_TYPE_INT32
            else:
                array_type = GGUFReader.GGUF_TYPE_INT32

            f.write(struct.pack('<I', array_type))
            f.write(struct.pack('<Q', len(value)))
            for item in value:
                self._write_value(f, array_type, item)
        elif value_type == GGUFReader.GGUF_TYPE_UINT64:
            f.write(struct.pack('<Q', value))
        elif value_type == GGUFReader.GGUF_TYPE_INT64:
            f.write(struct.pack('<q', value))
        elif value_type == GGUFReader.GGUF_TYPE_FLOAT64:
            f.write(struct.pack('<d', value))


def fix_adapter_file(file_path: str):
    """Fix a GGUF adapter file by adding the required metadata."""
    input_path = Path(file_path)

    if not input_path.exists():
        print(f"Error: File not found: {input_path}")
        return False

    print(f"Processing: {input_path}")

    # Create backup
    backup_path = input_path.with_suffix('.gguf.backup')
    if not backup_path.exists():
        shutil.copy2(input_path, backup_path)
        print(f"Created backup: {backup_path}")

    try:
        # Read the GGUF file
        reader = GGUFReader(input_path)
        reader.read()

        # Check if it already has the correct type
        if 'general.type' in reader.metadata and reader.metadata['general.type'][1] == 'adapter':
            print("\n[OK] File already has 'general.type = adapter'. No changes needed.")
            return True

        # Write the fixed file to a temporary location first
        temp_path = input_path.with_suffix('.gguf.fixed')
        writer = GGUFWriter(reader)
        writer.write(temp_path)

        # Replace the original file
        shutil.move(str(temp_path), str(input_path))

        print(f"\n[SUCCESS] Fixed adapter file: {input_path}")
        print(f"  Added: general.type = adapter")
        return True

    except Exception as e:
        print(f"\nError processing file: {e}")
        if backup_path.exists():
            print(f"Backup available at: {backup_path}")
        return False


if __name__ == "__main__":
    # Default to the known adapter path
    adapter_path = r"C:\Users\Josh\AppData\Local\Lazarus\Models\LoRA-Adapters\Qwen3-Coder-30B-A3B-Instruct\adapter.gguf"

    if len(sys.argv) > 1:
        adapter_path = sys.argv[1]

    success = fix_adapter_file(adapter_path)
    sys.exit(0 if success else 1)