import os
import sys
import datetime

args = sys.argv
argc = len(args)

if argc < 2:
    print(f'Usage: {args[0]} [S3URI]')
    quit()

src = args[1]
s3uri = args[2]

print('Uploading uncompressed files...')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*.br" --exclude "*.gz"')

print('Uploading Brotli compressed files...')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*" --include "*.br" --content-encoding br')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*" --include "*.wasm.br" --content-encoding br --content-type "application/wasm"')

print('Uploading Gzip compressed files...')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*" --include "*.gz" --content-encoding gzip')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*" --include "*.wasm.gz" --content-encoding gzip --content-type "application/wasm"')
