import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import { createServer, request as forwardRequest } from 'node:http';
import { extname, resolve, sep } from 'node:path';
import { pipeline } from 'node:stream';
import { fileURLToPath } from 'node:url';

const webRoot = resolve(fileURLToPath(new URL('./public/', import.meta.url)));
const accountsApi = new URL(process.env.ACCOUNTS_API_URL ?? 'http://127.0.0.1:5081');
const tasksApi = new URL(process.env.TASKS_API_URL ?? 'http://127.0.0.1:5082');
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

function upstreamFor(path) {
  if (path.startsWith('/api/accounts/')) return accountsApi;
  if (path === '/api/health' || path === '/api/tasks' || path.startsWith('/api/tasks/')) {
    return tasksApi;
  }
}

function end(response, status) {
  response.writeHead(status).end();
}

function forwardedHeaders(headers) {
  const excluded = new Set([
    'connection', 'keep-alive', 'proxy-authenticate', 'proxy-authorization',
    'te', 'trailer', 'transfer-encoding', 'upgrade',
    ...String(headers.connection ?? '').toLowerCase().split(',').map((name) => name.trim()),
  ]);
  return Object.fromEntries(Object.entries(headers).filter(([name]) => !excluded.has(name)));
}

function proxy(request, response, target, path) {
  const upstream = forwardRequest({
    hostname: target.hostname,
    port: target.port || 80,
    path,
    method: request.method,
    headers: { ...forwardedHeaders(request.headers), host: target.host },
  }, (upstreamResponse) => {
    response.writeHead(upstreamResponse.statusCode, forwardedHeaders(upstreamResponse.headers));
    pipeline(upstreamResponse, response, () => {});
  });

  upstream.setTimeout(15_000, () => upstream.destroy(new Error('API request timed out')));
  upstream.on('error', () => {
    if (response.destroyed) return;
    if (response.headersSent) response.destroy();
    else end(response, 502);
  });
  response.on('close', () => {
    if (!response.writableEnded) upstream.destroy();
  });
  request.pipe(upstream);
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
  let url;
  let path;
  try {
    url = new URL(request.url, 'http://localhost');
    path = decodeURIComponent(url.pathname);
  } catch {
    return end(response, 400);
  }

  const target = upstreamFor(url.pathname);
  if (target) return proxy(request, response, target, url.pathname + url.search);
  if (path === '/api' || path.startsWith('/api/')) return end(response, 404);
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
