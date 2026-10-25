(() => {
  const root = document.getElementById("app"); if (!root) return;
  const code = root.dataset.code, required = root.dataset.required === "true";
  const token = document.querySelector('meta[name="csrf"]').content;
  const $ = s => root.querySelector(s);
  const labels = ["", "Çok kötü", "Kötü", "Orta", "İyi", "Harika"];
  const stars = [...root.querySelectorAll("#stars .star")];
  const comment = $("#comment"), name = $("#name"), email = $("#email");
  const show = id => ["form", "preview", "done"].forEach(x => { $("#" + x).hidden = x !== id; });
  let rating = 0, busy = false;

  stars.forEach((b, i) => b.addEventListener("click", () => {
    rating = i + 1;
    stars.forEach((s, j) => { s.classList.toggle("on", j < rating); s.setAttribute("aria-checked", String(j === i)); });
    b.classList.remove("pop"); void b.offsetWidth; b.classList.add("pop");
    $("#rlabel").textContent = labels[rating];
  }));
  comment.addEventListener("input", () => { $("#counter").textContent = comment.value.length + "/1500"; });

  $("#form").addEventListener("submit", e => {
    e.preventDefault(); const err = $("#err"); err.textContent = "";
    if (!rating) { err.textContent = "Lütfen bir puan seçin."; return; }
    if (required && !comment.value.trim()) { err.textContent = "Lütfen bir yorum yazın."; return; }
    if (email.value && !/^\S+@\S+\.\S+$/.test(email.value)) { err.textContent = "E-posta adresi geçersiz."; return; }
    $("#pvStars").textContent = "★".repeat(rating) + "☆".repeat(5 - rating);
    $("#pvLabel").textContent = labels[rating];
    const pc = $("#pvComment"); pc.textContent = comment.value.trim(); pc.hidden = !pc.textContent;
    show("preview");
  });
  $("#editBtn").addEventListener("click", () => { if (!busy) show("form"); });

  $("#sendBtn").addEventListener("click", async () => {
    if (busy) return; busy = true;
    const btn = $("#sendBtn"), err = $("#pvErr"); err.textContent = ""; btn.disabled = true;
    const label = btn.textContent; btn.innerHTML = '<span class="spin" role="status" aria-label="Gönderiliyor"></span>';
    try {
      const r = await fetch("/api/feedback", { method: "POST", headers: { "Content-Type": "application/json", "RequestVerificationToken": token },
        body: JSON.stringify({ code, rating, comment: comment.value.trim() || null, name: name.value.trim() || null, email: email.value.trim() || null, website: "" }) });
      const j = await r.json().catch(() => ({}));
      if (!r.ok) {
        err.textContent = j.error || (r.status === 429 ? "Çok fazla deneme. Lütfen biraz sonra tekrar deneyin." : "Gönderilemedi, lütfen tekrar deneyin.");
        btn.disabled = false; btn.textContent = label; busy = false; return;
      }
      show("done");
      if (j.google) {
        $("#googleBox").hidden = false; $("#googleLink").href = j.google;
        if (comment.value.trim()) $("#copyBtn").hidden = false;
        setTimeout(() => { window.location.href = j.google; }, 6000);
      } else if (j.googleMissing) { $("#missing").hidden = false; }
    } catch { err.textContent = "Bağlantı hatası. Lütfen tekrar deneyin."; btn.disabled = false; btn.textContent = label; busy = false; }
  });

  $("#copyBtn").addEventListener("click", async () => {
    const text = comment.value.trim(), b = $("#copyBtn");
    try { await navigator.clipboard.writeText(text); }
    catch { const t = document.createElement("textarea"); t.value = text; document.body.append(t); t.select(); document.execCommand("copy"); t.remove(); }
    b.classList.add("copied"); $("#copyTxt").textContent = "Kopyalandı";
    setTimeout(() => { b.classList.remove("copied"); $("#copyTxt").textContent = "Yorumumu kopyala"; }, 2500);
  });
})();
