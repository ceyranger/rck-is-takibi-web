/**
 * RCK Web Görüntüleme — Google Apps Script
 *
 * KURULUM (3 adım):
 * 1) Aşağıdaki DRIVE_FILE_ID satırına Drive'daki web-view-latest.json dosya ID'sini yapıştır.
 * 2) kurulumYap fonksiyonunu bir kez Çalıştır (izin ver).
 * 3) Dağıt > Yeni dağıtım > Web uygulaması > Yürüt: Ben > Erişim: Herkes
 *
 * Site her istekte fileId parametresi gonderebilir; Script properties yedektir.
 * Telefonda gireceğin PIN: 271179
 */

// === BURAYI DOLDUR ===
var DRIVE_FILE_ID = "1wJyu-kSWEG7YsFjSHmGAJ4RpRmFGvH_C";
var WEB_PIN = "271179";
var WEB_PIN_HASH = "A5AE5D5D7920AD56D6053C5691C0DBC06F94575EC9638BE37463A0E427780456";

var RATE_LIMIT_MAX = 5;
var RATE_LIMIT_WINDOW_MS = 60 * 1000;

/** Bir kez çalıştır — Script properties'e kaydeder. */
function kurulumYap() {
  if (!DRIVE_FILE_ID || DRIVE_FILE_ID.indexOf("BURAYA") >= 0) {
    throw new Error("Önce DRIVE_FILE_ID satırına Drive dosya ID yaz.");
  }

  PropertiesService.getScriptProperties().setProperties({
    DRIVE_FILE_ID: DRIVE_FILE_ID,
    PIN_HASH: WEB_PIN_HASH
  });

  Logger.log("Kurulum tamam. Telefonda PIN: " + WEB_PIN);
}

function doGet(e) {
  return respond_(e);
}

function doPost(e) {
  return respond_(e);
}

function respond_(e) {
  var params = (e && e.parameter) ? e.parameter : {};
  var payload = buildPayload_(params);
  var json = JSON.stringify(payload);
  var callback = String(params.callback || "").trim();
  if (callback && /^[A-Za-z0-9_]+$/.test(callback)) {
    return ContentService
      .createTextOutput(callback + "(" + json + ");")
      .setMimeType(ContentService.MimeType.JAVASCRIPT);
  }
  return ContentService.createTextOutput(json).setMimeType(ContentService.MimeType.JSON);
}

function buildPayload_(params) {
  var pin = String(params.pin || "").trim();
  if (!pin) {
    return { error: "PIN gerekli.", httpStatus: 401 };
  }

  if (!checkRateLimit_(pin)) {
    return { error: "Çok fazla deneme. Bir dakika bekleyin.", httpStatus: 429 };
  }

  var props = PropertiesService.getScriptProperties();
  var expectedHash = String(props.getProperty("PIN_HASH") || WEB_PIN_HASH).trim().toUpperCase();
  var fileId = String(params.fileId || props.getProperty("DRIVE_FILE_ID") || DRIVE_FILE_ID).trim();
  if (!expectedHash) {
    return { error: "Kurulum eksik. kurulumYap calistir.", httpStatus: 500 };
  }
  if (!fileId || fileId.indexOf("BURAYA") >= 0) {
    return { error: "Drive dosya kimligi gerekli (siteden kaynak secin).", httpStatus: 400 };
  }

  if (hashPin_(pin) !== expectedHash) {
    return { error: "Geçersiz PIN.", httpStatus: 401 };
  }

  try {
    var file = DriveApp.getFileById(fileId);
    var content = file.getBlob().getDataAsString("UTF-8");
    var parsed = JSON.parse(content);
    if (parsed.kind !== "web-view") {
      return { error: "Beklenmeyen dosya türü.", httpStatus: 422 };
    }
    return parsed;
  } catch (err) {
    return { error: "Dosya okunamadı: " + err, httpStatus: 404 };
  }
}

function handleRequest_(e) {
  return respond_(e);
}

function hashPin_(pin) {
  var digest = Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, pin, Utilities.Charset.UTF_8);
  return bytesToHex_(digest).toUpperCase();
}

function bytesToHex_(bytes) {
  return bytes.map(function (b) {
    var v = (b < 0 ? b + 256 : b).toString(16);
    return v.length === 1 ? "0" + v : v;
  }).join("");
}

function checkRateLimit_(pin) {
  var cache = CacheService.getScriptCache();
  var key = "pin_attempts_" + hashPin_(pin).slice(0, 16);
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
  if (typeof payload === "object" && payload !== null) {
    payload.httpStatus = status;
  }
  return output;
}
