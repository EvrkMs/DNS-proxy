// wwwroot/js/site.js
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('tr[data-href]')
        .forEach(tr => tr.onclick = () => location = tr.dataset.href);
});