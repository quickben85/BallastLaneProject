import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import { createServer } from 'node:http';
import { extname, resolve, sep } from 'node:path';
import { pipeline } from 'node:stream';
import { fileURLToPath } from 'node:url';

const webRoot = resolve(fileURLToPath(new URL('./public/', import.meta.url)));
const mediaTypes = {
  '.css': 'text/css',
  '.html': 'text/html; charset=utf-8',
  '.ico': 'image/x-icon',
  '.js': 'text/javascript',
  '.json': 'application/json',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.woff2': 'font/woff2',
};

function end(response, status) {
  response.writeHead(status).end();
}

async function fileInfo(path) {
  try {
    const info = await stat(path);
    return info.isFile() ? info : undefined;
  } catch (error) {
    if (error.code !== 'ENOENT' && error.code !== 'ENOTDIR') throw error;
  }
}

async function serve(request, response) {
  let path;
  try {
    const url = new URL(request.url, 'http://localhost');
    path = decodeURIComponent(url.pathname);
  } catch {
    return end(response, 400);
  }

  if (request.method !== 'GET' && request.method !== 'HEAD') return end(response, 405);

  let file = resolve(webRoot, '.' + path);
  if (file !== webRoot && !file.startsWith(webRoot + sep)) return end(response, 404);

  let info = await fileInfo(file);
  if (!info && (path === '/' || (!extname(path) && request.headers.accept?.includes('text/html')))) {
    file = resolve(webRoot, 'index.html');
    info = await fileInfo(file);
  }
  if (!info) return end(response, 404);

  response.writeHead(200, {
    'Content-Type': mediaTypes[extname(file)] ?? 'application/octet-stream',
    'Content-Length': info.size,
  });
  if (request.method === 'HEAD') return response.end();
  pipeline(createReadStream(file), response, () => {});
}

createServer((request, response) => {
  serve(request, response).catch((error) => {
    console.error(error);
    if (response.headersSent) response.destroy();
    else end(response, 500);
  });
}).listen(8080, '0.0.0.0');
