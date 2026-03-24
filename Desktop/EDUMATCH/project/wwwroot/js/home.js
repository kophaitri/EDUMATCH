// home page scripts extracted from Index.cshtml
// Scroll reveal
const edumatchObserver = new IntersectionObserver(
  (entries) =>
    entries.forEach((e) => {
      if (e.isIntersecting) {
        e.target.classList.add("visible");
        edumatchObserver.unobserve(e.target);
      }
    }),
  { threshold: 0.12 },
);
document.addEventListener("DOMContentLoaded", () => {
  document
    .querySelectorAll(".reveal")
    .forEach((el) => edumatchObserver.observe(el));

  // Nav scroll shadow
  window.addEventListener("scroll", () => {
    const nav = document.querySelector(".nav");
    if (!nav) return;
    nav.style.boxShadow =
      window.scrollY > 20 ? "0 2px 24px rgba(0,0,0,0.08)" : "none";
  });
});
