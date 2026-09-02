const DATA_OBJECT_KEY = "web-view-latest.json";

export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    if (request.method === "OPTIONS") {
      return corsResponse(null, 204);
    }

    if (url.pathname === "/" || url.pathname === "") {
      return corsResponse(
        JSON.stringify({
          ok: true,
          message: "RCK İş Takibi API",
          dataEndpoint: "/api/data",
        }),
        200
      );
    }

    if (url.pathname === "/api/data" || url.pathname === "/api/data/") {
      if (request.method === "GET") {
        return handleGet(request, env);
      }
      if (request.method === "PUT") {
        return handlePut(request, env);
      }
    }

    return corsResponse(JSON.stringify({ error: "Not found" }), 404);
  },
};

async function handleGet(request, env) {
  const pin = normalizePin(request.headers.get("X-Web-Pin"));
  if (!isValidPin(pin, env)) {
    return corsResponse(JSON.stringify({ error: "Geçersiz PIN." }), 401);
  }

  const object = await env.DATA_BUCKET.get(DATA_OBJECT_KEY);
  if (!object) {
    return corsResponse(JSON.stringify({ error: "Veri henüz yok." }), 404);
  }

  return corsResponse(object.body, 200, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
  });
}

async function handlePut(request, env) {
  const apiKey = String(request.headers.get("X-API-Key") || "").trim();
  if (!apiKey || apiKey !== String(env.UPLOAD_API_KEY || "").trim()) {
    return corsResponse(JSON.stringify({ error: "Yetkisiz." }), 401);
  }

  const contentType = String(request.headers.get("Content-Type") || "").toLowerCase();
  if (!contentType.includes("application/json")) {
    return corsResponse(JSON.stringify({ error: "Content-Type application/json gerekli." }), 415);
  }

  await env.DATA_BUCKET.put(DATA_OBJECT_KEY, request.body, {
    httpMetadata: { contentType: "application/json; charset=utf-8" },
  });

  return corsResponse(JSON.stringify({ ok: true }), 200);
}

function isValidPin(pin, env) {
  if (!pin) {
    return false;
  }

  const webPin = normalizePin(env.WEB_PIN || "271179");
  const adminPin = normalizePin(env.ADMIN_PIN || "0258");
  return pin === webPin || pin === adminPin;
}

function normalizePin(value) {
  return String(value || "").replace(/\s+/g, "").trim();
}

function corsResponse(body, status = 200, extraHeaders = {}) {
  const headers = {
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "GET, PUT, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type, X-Web-Pin, X-API-Key",
    ...extraHeaders,
  };

  return new Response(body, { status, headers });
}
