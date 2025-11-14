Error correction in .sga files is different between older versions of SGA files (v2 - v4), where the entire archive is verified by checksum and newer versions, where each file is verified separately. 

# Older error correction (V2 - V5)
This method uses two checksums located in the archive header: File hash and TOC hash.

## File hash
File hash is an MD5 hash used to validate the entire contents of the archive. It starts after the archive header and contains the rest of the file, including the TOC. 

The hash uses an eigenvalue of `E01519D6-2DB7-4640-AF54-0A23319C56C3`.

## TOC hash
TOC hash is an MD5 hash used to validate the integrity of the table of contents. 

The hash uses an eigenvalue of `DFC9AF62-FC1B-4180-BC27-11CCE87D3EFF`.

# Newer error correction (V7 - ?)
This method applies to each file separately. You can choose between different error correction options using the Verification type attribute in the file definition. Available options are:

- 0 - None 
- 1 - CRC
- 2 - Block CRC
- 3 - Block MD5
- 4 - Block SHA-1

## None
The file is not validated.

## CRC
The entire file contents is validated using CRC-32. The resulting value is stored in the CRC attribute in the file definition. CRC is always present even when the file validation is not set to CRC.

## Blocks
All of these options divide the contents of the compressed file into blocks. Each block has the maximum size set by the block size value in the TOC header. If the file is smaller than 1 block or does not fit into blocks, the last block will be smaller. Each block is hashed separately, and the resulting hash is stored in the hash array. The offset of the first hash in the hash array for a file is stored in the file definition hash offset value. 

Available options are:
- Block CRC - Using standard CRC to hash blocks.
- Block MD5 - Using MD5 to hash blocks.
- Block SHA-1 - Using SHA-1 to hash blocks.