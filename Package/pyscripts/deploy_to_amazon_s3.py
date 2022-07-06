import os
import sys
import datetime

args = sys.argv
argc = len(args)

if argc < 3:
    print(f'Usage: {args[0]} [S3URI]')
    quit()

profile = args[1]
region = args[2]
src = args[3]
s3uri = args[4]

print('Uploading uncompressed files...')
os.system(f'aws s3 cp --profile {profile} --region {region} {src} {s3uri} --recursive --exclude "*.br" --exclude "*.gz"')

print('Uploading Brotli compressed files...')
os.system(f'aws s3 cp --profile {profile} --region {region} {src} {s3uri} --recursive --exclude "*" --include "*.br" --content-encoding br')
os.system(f'aws s3 cp --profile {profile} --region {region} {src} {s3uri} --recursive --exclude "*" --include "*.wasm.br" --content-encoding br --content-type "application/wasm"')

print('Uploading Gzip compressed files...')
os.system(f'aws s3 cp --profile {profile} --region {region} {src} {s3uri} --recursive --exclude "*" --include "*.gz" --content-encoding gzip')
os.system(f'aws s3 cp --profile {profile} --region {region} {src} {s3uri} --recursive --exclude "*" --include "*.wasm.gz" --content-encoding gzip --content-type "application/wasm"')
