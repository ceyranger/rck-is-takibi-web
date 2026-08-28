/**
 * Google Apps Script Web App proxy for web-view-latest.json
 *
 * Setup:
 * 1. script.google.com -> New project -> paste this file
 * 2. Project Settings -> Script properties:
 *    - DRIVE_FILE_ID = Google Drive file id for web-view-latest.json
 *    - PIN_HASH = SHA-256 hex of your PIN (uppercase)
 * 3. Deploy -> New deployment -> Web app
 *    - Execute as: Me
 *    - Who has access: Anyone
 * 4. Copy deployment URL into web/config.js
 */

var RATE_LIMIT_MAX = 5;
var RATE_LIMIT_WINDOW_MS = 60 * 1000;

function doGet(e) {
  return handleRequest_(e);
}

function doPost(e) {
  return handleRequest_(e);
}

function handleRequest_(e) {
  var params = (e && e.parameter) ? e.parameter : {};
  var pin = String(params.pin || '').trim();
  if (!pin) {
    return jsonResponse_(401, { error: 'PIN gerekli.' });
  }

  if (!checkRateLimit_(pin)) {
    return jsonResponse_(429, { error: 'Çok fazla deneme. Bir dakika bekleyin.' });
  }

  var props = PropertiesService.getScriptProperties();
  var expectedHash = String(props.getProperty('PIN_HASH') || '').trim().toUpperCase();
  var fileId = String(props.getProperty('DRIVE_FILE_ID') || '').trim();
  if (!expectedHash || !fileId) {
    return jsonResponse_(500, { error: 'Sunucu yapılandırması eksik.' });
  }

  if (hashPin_(pin) !== expectedHash) {
    return jsonResponse_(401, { error: 'Geçersiz PIN.' });
  }

  try {
    var file = DriveApp.getFileById(fileId);
    var content = file.getBlob().getDataAsString('UTF-8');
    var parsed = JSON.parse(content);
    if (parsed.kind !== 'web-view') {
      return jsonResponse_(422, { error: 'Beklenmeyen dosya türü.' });
    }

    return ContentService
      .createTextOutput(JSON.stringify(parsed))
      .setMimeType(ContentService.MimeType.JSON);
  } catch (err) {
    return jsonResponse_(404, { error: 'Dosya okunamadı: ' + err });
  }
}

function hashPin_(pin) {
  var digest = Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, pin, Utilities.Charset.UTF_8);
  return bytesToHex_(digest).toUpperCase();
}

function bytesToHex_(bytes) {
  return bytes.map(function (b) {
    var v = (b < 0 ? b + 256 : b).toString(16);
    return v.length === 1 ? '0' + v : v;
  }).join('');
}

function checkRateLimit_(pin) {
  var cache = CacheService.getScriptCache();
  var key = 'pin_attempts_' + hashPin_(pin).slice(0, 16);
  var raw = cache.get(key);
  var count = raw ? parseInt(raw, 10) : 0;
  if (count >= RATE_LIMIT_MAX) {
    return false;
  }
  cache.put(key, String(count + 1), Math.ceil(RATE_LIMIT_WINDOW_MS / 1000));
  return true;
}

function jsonResponse_(status, payload) {
  var output = ContentService.createTextOutput(JSON.stringify(payload))
    .setMimeType(ContentService.MimeType.JSON);
  // Apps Script Web App cannot set HTTP status codes reliably; include status field.
  if (typeof payload === 'object' && payload !== null) {
    payload.httpStatus = status;
  }
  return output;
}

/** Run once in editor to generate PIN_HASH for a PIN, then store in Script properties. */
function generatePinHashForSetup() {
  var pin = '1234'; // change before running, then delete from source
  Logger.log(hashPin_(pin));
}
