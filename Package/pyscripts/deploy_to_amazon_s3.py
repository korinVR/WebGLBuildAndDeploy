import os
import datetime
import webbrowser

timestamp = datetime.datetime.now().strftime('%Y%m%d%H%M%S')

src = './dist/WebGL'
uri = f'korinvr.com/bin/WebGLBuildToolsTest/{timestamp}/'

s3uri = 's3://' + uri

print(os.getcwd())

# input('Hello, Python!')
# quit()

print('Uploading uncompressed files...')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*.br" --exclude "*.gz"')

print('Uploading Brotli compressed files...')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*" --include "*.br" --content-encoding br')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*" --include "*.wasm.br" --content-encoding br --content-type "application/wasm"')

print('Uploading Gzip compressed files...')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*" --include "*.gz" --content-encoding gzip')
os.system(f'aws s3 cp --region ap-northeast-1 {src} {s3uri} --recursive --exclude "*" --include "*.wasm.gz" --content-encoding gzip --content-type "application/wasm"')

webbrowser.open('https://' + uri)
