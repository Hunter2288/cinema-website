const API = "http://localhost:5289/api";

// ---------- користувач ----------
function currentUser() {
  try { return JSON.parse(localStorage.getItem("user") || sessionStorage.getItem("user") || "null"); }
  catch { return null; }
}
function logout() {
  localStorage.removeItem("user"); sessionStorage.removeItem("user");
  location.href = "index.html";
}
const USER = currentUser();
const USER_ID = USER ? USER.id : null;

// ---------- запити ----------
async function api(path, options) {
  const res = await fetch(API + path, {
    ...options,
    headers: options && options.body ? { "Content-Type": "application/json" } : undefined,
  });
  if (!res.ok) {
    let detail = "";
    try { detail = (await res.text()).slice(0, 200); } catch {}
    const err = new Error(`Помилка ${res.status}${detail ? ": " + detail : ""}`);
    err.status = res.status; err.detail = detail;
    throw err;
  }
  return res.status === 204 ? null : res.json().catch(() => null);
}

// ---------- форматування ----------
const esc = s => String(s ?? "").replace(/[&<>"']/g, c =>
  ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const fmtTime = d => new Date(d).toLocaleTimeString("uk-UA", { hour: "2-digit", minute: "2-digit" });
const fmtDate = d => new Date(d).toLocaleDateString("uk-UA", { day: "numeric", month: "long" });
const fmtDateFull = d => new Date(d).toLocaleDateString("uk-UA", { day: "numeric", month: "long", year: "numeric" }).replace(/\s?р\.$/, "");
const dayKey = d => { const x = new Date(d); return `${x.getFullYear()}-${String(x.getMonth() + 1).padStart(2, "0")}-${String(x.getDate()).padStart(2, "0")}`; };
const price = p => `${Number(p).toFixed(0)} грн`;
function durText(min) {
  const h = Math.floor(min / 60), m = min % 60;
  return (h ? `${h} год` : "") + (h && m ? " " : "") + (m ? `${m} хв` : "");
}
const stars = n => "★".repeat(n) + "☆".repeat(5 - n);
const genresOf = m => (m.genre || "").split(",").map(g => g.trim()).filter(Boolean);

const CLOCK = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#f5c518" stroke-width="2"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>';

function toast(text) {
  let t = document.getElementById("toast");
  if (!t) { t = document.createElement("div"); t.id = "toast"; document.body.appendChild(t); }
  t.textContent = text;
  t.classList.add("show");
  clearTimeout(toast._t);
  toast._t = setTimeout(() => t.classList.remove("show"), 2800);
}

function posterHtml(m) {
  return m.posterUrl
    ? `<img src="${esc(m.posterUrl)}" alt="${esc(m.title)}" loading="lazy">`
    : esc((m.title || "?").charAt(0));
}

function showError(el, e) {
  el.innerHTML = `<p class="msg err">Не вдалося завантажити дані. Перевірте, що API запущено (${esc(API)}). ${esc(e.message)}</p>`;
}

function ticketStatus(s) {
  const v = typeof s === "string" ? s.toLowerCase() : ["pending", "paid", "cancelled"][s];
  return {
    pending:   { cls: "",          text: "Очікує оплати" },
    paid:      { cls: "paid",      text: "Оплачено" },
    cancelled: { cls: "cancelled", text: "Скасовано" },
  }[v] || { cls: "", text: String(s) };
}
const isCancelled = s => ticketStatus(s).cls === "cancelled";

// ---------- картки ----------
function metaHtml(m) {
  return `${m.rating != null ? `<span class="rate"><b class="imdb">IMDb</b> ${esc(m.rating)}<small>/10</small></span>` : ""}
          <span>${CLOCK} ${durText(m.durationMinutes)}</span>`;
}

// велика картка «Кінотеатр» (головна)
function cinemaCard(m) {
  return `<article class="ccard">
    <a class="cpic" href="film.html?id=${m.id}">${posterHtml(m)}</a>
    <div class="cbody">
      <h3>${esc(m.title)}</h3>
      <div class="csub">${m.rating != null ? `<span class="ystar">★</span> ${esc(m.rating)} · ` : ""}${durText(m.durationMinutes)}</div>
      <div class="gtags">${genresOf(m).slice(0, 3).map(g => `<span class="gtag">${esc(g)}</span>`).join("")}</div>
      <div class="cbtns">
        <a class="btn dark" href="film.html?id=${m.id}">Детальніше</a>
        <a class="btn" href="booking.html?movie=${m.id}">Квитки</a>
      </div>
    </div>
  </article>`;
}

// картка з плашкою рейтингу/тривалості (каталог, «скоро у прокаті»)
function filmCard(m, soon) {
  return `<a class="film-card" href="film.html?id=${m.id}">
    <div class="pic">${posterHtml(m)}
      <div class="info">
        <span>${m.rating != null ? `<b class="imdb">IMDb</b>${esc(m.rating)}` : ""}</span>
        <span>${CLOCK} ${durText(m.durationMinutes)}</span>
        ${soon && m.releaseDate ? `<span class="date">${fmtDate(m.releaseDate)}</span>` : ""}
      </div>
    </div>
    <div class="name">${esc(m.title)}</div>
  </a>`;
}

function wireRail(root) {
  root.querySelectorAll(".railwrap").forEach(w => {
    const r = w.querySelector(".rail");
    w.querySelector(".l").onclick = () => r.scrollBy({ left: -r.clientWidth, behavior: "smooth" });
    w.querySelector(".r").onclick = () => r.scrollBy({ left: r.clientWidth, behavior: "smooth" });
    const upd = () => w.classList.toggle("noscroll", r.scrollWidth <= r.clientWidth + 2);
    upd();
    addEventListener("resize", upd);
  });
}

// ---------- шапка і підвал ----------
function header(active) {
    const right = USER
    ? `<a class="avatar" href="profile.html" title="Мій профіль">${USER.avatarUrl ? `<img src="${esc(USER.avatarUrl)}" alt="">` : esc((USER.name || "?").charAt(0).toUpperCase())}</a>`
    : `<a class="btn dark" href="login.html">Увійти</a>
       <a class="btn" href="login.html?tab=register">Реєстрація</a>`;
  document.getElementById("header").innerHTML = `
    <header class="top"><div class="wrap">
      <a class="brand" href="index.html"><img src="images/logo.png" alt="" onerror="this.remove()"><span>Показ тіней</span></a>
      <nav class="hnav">
        <div class="dd">
          <button class="ddbtn" type="button">Фільми <i>▾</i></button>
          <div class="ddmenu"><a href="catalog.html">Зараз у прокаті</a><a href="index.html#soon">Скоро у прокаті</a></div>
        </div>
        <a href="index.html#schedule">Розклад сеансів</a>
        <a href="index.html#soon">Незабаром у кіно</a>
      </nav>
      <form class="hsearch" role="search" onsubmit="event.preventDefault();location.href='catalog.html?q='+encodeURIComponent(this.q.value.trim())">
        <input name="q" placeholder="Пошук фільмів" aria-label="Пошук фільмів">
      </form>
      <a class="hlink" href="private.html">Приватний показ</a>
      <div class="hauth">${right}</div>
    </div></header>`;
}

const ICON = (body) => `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${body}</svg>`;
const ICONS = {
  phone: ICON('<path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>'),
  mail: ICON('<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/>'),
  pin: ICON('<path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/>'),
  fb: ICON('<path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"/>'),
  ig: ICON('<rect x="2" y="2" width="20" height="20" rx="5"/><path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/><line x1="17.5" y1="6.5" x2="17.51" y2="6.5"/>'),
  tg: ICON('<line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/>'),
  yt: ICON('<path d="M22.54 6.42a2.78 2.78 0 0 0-1.94-2C18.88 4 12 4 12 4s-6.88 0-8.6.46a2.78 2.78 0 0 0-1.94 2A29 29 0 0 0 1 11.75a29 29 0 0 0 .46 5.33A2.78 2.78 0 0 0 3.4 19c1.72.46 8.6.46 8.6.46s6.88 0 8.6-.46a2.78 2.78 0 0 0 1.94-2 29 29 0 0 0 .46-5.25 29 29 0 0 0-.46-5.33z"/><polygon points="9.75 15.02 15.5 11.75 9.75 8.48 9.75 15.02"/>'),
};

function footer() {
  const el = document.getElementById("footer");
  if (!el) return;
  el.innerHTML = `<footer class="big"><div class="wrap">
    <div class="foot4">
      <div>
        <a class="fbrand" href="index.html"><img src="images/logo.png" alt="" onerror="this.remove()"><span>Показ тіней</span></a>
        <p>Найкращий кінотеатр у місті з сучасним обладнанням та комфортними залами.</p>
      </div>
      <div><h4>Контакти</h4><ul>
        <li>${ICONS.phone} +38 (0XX) XXX XX XX</li>
        <li>${ICONS.mail} info@pokaz-tinei.ua</li>
        <li>${ICONS.pin} м. Чернівці</li></ul></div>
      <div><h4>Навігація</h4><ul>
        <li><a href="catalog.html">Зараз у прокаті</a></li>
        <li><a href="catalog.html">Забронювати квиток</a></li>
        <li><a href="private.html">Приватний показ</a></li>
        <li><a href="index.html">Про нас</a></li></ul></div>
      <div><h4>Соціальні мережі</h4>
        <div class="soc"><span>${ICONS.fb}</span><span>${ICONS.ig}</span><span>${ICONS.tg}</span><span>${ICONS.yt}</span></div></div>
    </div>
    <div class="copy">© 2025 Показ тіней. Всі права захищені.</div>
  </div></footer>`;
}
// ---------- місця, кроки, QR, телефон ----------
const ROWS_N = 5, SEATS_N = 8, VIP_ROW = 5, VIP_EXTRA = 50, FEE_RATE = 0.05;
const seatCode = (r, s) => `${r}-${s}`;
const parseSeat = c => { const [r, s] = String(c).split("-").map(Number); return { r, s }; };
const isVipSeat = c => parseSeat(c).r === VIP_ROW;
const orderLabel = n => String(n || "").replace(/^PT-/, "ПТ-");

function seatsText(codes) {
  const by = new Map();
  [...codes].map(parseSeat).sort((a, b) => a.r - b.r || a.s - b.s).forEach(x => {
    if (!by.has(x.r)) by.set(x.r, []);
    by.get(x.r).push(x.s);
  });
  return [...by].map(([r, ss]) => `Ряд ${r}, ${ss.length > 1 ? "місця" : "місце"} ${ss.join(", ")}`).join("; ");
}

function stepper(n) {
  const L = ["Вибір місць", "Контакти", "Оплата", "Квиток"];
  return `<div class="steps">${L.map((t, i) =>
    `${i ? '<span class="step-line"></span>' : ""}<span class="step ${i < n ? "on" : ""}"><i>${i + 1}</i>${t}</span>`).join("")}</div>`;
}

function drawQR(el, text, size) {
  if (window.QRCode) {
    el.innerHTML = "";
    new QRCode(el, { text, width: size, height: size, colorDark: "#000000", colorLight: "#ffffff", correctLevel: QRCode.CorrectLevel.M });
  } else {
    el.textContent = text;
  }
}

function maskPhone(el) {
  const fmt = v => {
    let d = v.replace(/\D/g, "");
    if (d.startsWith("380")) d = d.slice(3); else if (d.startsWith("0")) d = d.slice(1);
    d = d.slice(0, 9);
    const p = [d.slice(0, 2), d.slice(2, 5), d.slice(5, 7), d.slice(7, 9)].filter(Boolean);
    return "+380" + (p.length ? " " + p.join(" ") : "");
  };
  el.addEventListener("input", () => { el.value = fmt(el.value); });
  el.addEventListener("focus", () => { if (!el.value) el.value = "+380 "; });
}
const phoneOk = v => /^\+380 \d{2} \d{3} \d{2} \d{2}$/.test(v);
const emailOk = v => /^\S+@\S+\.\S+$/.test(v);
const cap = s => s.charAt(0).toUpperCase() + s.slice(1);