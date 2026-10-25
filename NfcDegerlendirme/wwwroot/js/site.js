// CSP inline script'e izin vermediği için onay penceresi burada.
document.addEventListener("submit", e => {
  const m = e.target.dataset && e.target.dataset.confirm;
  if (m && !confirm(m)) e.preventDefault();
});

// Profil fotoğrafı: dosya seçilince doğrula, önizle ve otomatik yükle (tek tıkla).
(() => {
  const input = document.getElementById("logoInput"); if (!input) return;
  const form = document.getElementById("logoForm"), btn = document.getElementById("logoBtn"),
        err = document.getElementById("logoErr"), avatar = document.getElementById("logoAvatar");
  btn.addEventListener("keydown", e => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); input.click(); } });
  input.addEventListener("change", () => {
    err.textContent = ""; const f = input.files && input.files[0]; if (!f) return;
    if (!["image/png", "image/jpeg", "image/webp"].includes(f.type)) { err.textContent = "Yalnızca PNG, JPG veya WebP yüklenebilir."; input.value = ""; return; }
    if (f.size > 2 * 1024 * 1024) { err.textContent = "Dosya en fazla 2 MB olabilir."; input.value = ""; return; }
    const img = document.createElement("img"); img.alt = "Önizleme"; img.src = URL.createObjectURL(f);
    avatar.replaceChildren(img); btn.textContent = "Yükleniyor…"; btn.setAttribute("aria-busy", "true");
    form.submit();
  });
})();
