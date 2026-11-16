const tracks = [
  { title: 'A Brighter Day', artist: 'Soundscape', album: 'A Higher Place', duration: 245, file: '' },
  { title: 'Lost in the Light', artist: 'Soundscape', album: 'A Higher Place', duration: 208, file: '' },
  { title: 'Better Together', artist: 'Cora Vane', album: 'A Higher Place', duration: 252, file: '' },
  { title: 'Skyline', artist: 'Mira Sol', album: 'A Higher Place', duration: 236, file: '' },
  { title: 'Moments', artist: 'Eli North', album: 'A Higher Place', duration: 260, file: '' },
  { title: 'Into the Blue', artist: 'Tidal Echo', album: 'A Higher Place', duration: 228, file: '' },
  { title: 'Home Again', artist: 'The Bay', album: 'A Higher Place', duration: 301, file: '' },
  { title: 'A Higher Place', artist: 'Soundscape', album: 'A Higher Place', duration: 273, file: '' }
];
let currentIndex = 0;
const audio = document.querySelector('#audio');
const playButton = document.querySelector('#play');
const seek = document.querySelector('#seek');
const elapsed = document.querySelector('#elapsed');
const durationLabel = document.querySelector('#duration');
const formatTime = seconds => `${Math.floor(seconds / 60)}:${String(Math.floor(seconds % 60)).padStart(2, '0')}`;

function renderTrack(index, autoplay = false) {
  currentIndex = (index + tracks.length) % tracks.length;
  const track = tracks[currentIndex];
  document.querySelector('#track-title').textContent = track.title;
  document.querySelector('#track-artist').childNodes[0].textContent = `${track.artist} `;
  document.querySelector('#track-album').textContent = track.album;
  durationLabel.textContent = formatTime(track.duration);
  seek.max = track.duration;
  seek.value = 0;
  elapsed.textContent = '0:00';
  document.querySelectorAll('.track').forEach((row, i) => {
    row.classList.toggle('selected', i === currentIndex);
    row.querySelector('.track-number').textContent = i === currentIndex ? '▮' : String(i + 1);
  });
  audio.pause();
  audio.removeAttribute('src');
  if (track.file) audio.src = track.file;
  playButton.textContent = '▶';
  playButton.setAttribute('aria-label', 'Reproducir');
  if (autoplay && track.file) audio.play().catch(() => {});
}

document.querySelectorAll('.track').forEach(row => row.addEventListener('click', () => renderTrack(Number(row.dataset.index), true)));
playButton.addEventListener('click', () => {
  if (!tracks[currentIndex].file) {
    document.querySelector('#file-picker').click();
    return;
  }
  if (audio.paused) audio.play().catch(() => {}); else audio.pause();
});
document.querySelector('#next').addEventListener('click', () => renderTrack(currentIndex + 1, true));
document.querySelector('#previous').addEventListener('click', () => renderTrack(currentIndex - 1, true));
audio.addEventListener('play', () => { playButton.textContent = 'Ⅱ'; playButton.setAttribute('aria-label', 'Pausar'); });
audio.addEventListener('pause', () => { playButton.textContent = '▶'; playButton.setAttribute('aria-label', 'Reproducir'); });
audio.addEventListener('timeupdate', () => {
  if (!Number.isFinite(audio.duration)) return;
  seek.max = Math.floor(audio.duration);
  seek.value = Math.floor(audio.currentTime);
  elapsed.textContent = formatTime(audio.currentTime);
  durationLabel.textContent = formatTime(audio.duration);
});
audio.addEventListener('ended', () => renderTrack(currentIndex + 1, true));
seek.addEventListener('input', () => {
  elapsed.textContent = formatTime(Number(seek.value));
  if (Number.isFinite(audio.duration)) audio.currentTime = Number(seek.value);
});
document.querySelector('#add-music').addEventListener('click', () => document.querySelector('#file-picker').click());
document.querySelector('#file-picker').addEventListener('change', event => {
  const files = [...event.target.files];
  if (!files.length) return;
  const start = tracks.length;
  files.forEach(file => tracks.push({ title: file.name.replace(/\.[^.]+$/, ''), artist: 'Tu biblioteca', album: file.type.startsWith('video/') ? 'Vídeos' : 'Archivos locales', duration: 0, file: URL.createObjectURL(file) }));
  const list = document.querySelector('#track-list');
  files.forEach((file, offset) => {
    const index = start + offset;
    const row = document.createElement('button');
    row.className = 'track'; row.dataset.index = index;
    row.innerHTML = `<span class="track-number">${index + 1}</span><span class="track-details"><strong></strong><small>Tu biblioteca</small></span><time>--:--</time>`;
    row.querySelector('strong').textContent = file.name.replace(/\.[^.]+$/, '');
    row.addEventListener('click', () => renderTrack(index, true));
    list.append(row);
  });
  renderTrack(start, true);
});
document.querySelectorAll('.nav-link').forEach(button => button.addEventListener('click', () => {
  document.querySelectorAll('.nav-link').forEach(item => item.classList.remove('selected'));
  button.classList.add('selected');
  document.querySelector('#page-title').textContent = button.dataset.page;
}));
document.querySelectorAll('.browse-link').forEach(button => button.addEventListener('click', () => {
  document.querySelectorAll('.browse-link').forEach(item => item.classList.remove('active'));
  button.classList.add('active');
  document.querySelector('#view-select').value = button.dataset.view;
}));
document.querySelector('#view-select').addEventListener('change', event => {
  document.querySelectorAll('.browse-link').forEach(item => item.classList.toggle('active', item.dataset.view === event.target.value));
});
document.querySelector('#search').addEventListener('input', event => {
  const query = event.target.value.trim().toLocaleLowerCase();
  document.querySelectorAll('.track').forEach(row => { row.hidden = !row.textContent.toLocaleLowerCase().includes(query); });
});
document.querySelector('#favorite').addEventListener('click', event => {
  const button = event.currentTarget;
  const liked = button.classList.toggle('liked');
  button.textContent = liked ? '♥' : '♡';
  button.setAttribute('aria-label', liked ? 'Quitar de favoritos' : 'Añadir a favoritos');
});
document.querySelector('#shuffle').addEventListener('click', event => event.currentTarget.classList.toggle('active'));
document.querySelector('#repeat').addEventListener('click', event => event.currentTarget.classList.toggle('active'));
document.querySelectorAll('.tab').forEach(button => button.addEventListener('click', () => {
  document.querySelectorAll('.tab').forEach(item => item.classList.remove('active'));
  button.classList.add('active');
  const content = document.querySelector('#tab-content');
  if (button.dataset.tab === 'Lista') content.hidden = false;
  else {
    content.hidden = false;
    const heading = content.querySelector('.album-heading h2');
    const original = heading.textContent;
    heading.textContent = button.dataset.tab === 'Letras' ? 'Letras no disponibles' : 'Información de la pista';
    content.querySelector('.album-heading span').textContent = button.dataset.tab === 'Letras' ? 'Las letras aparecerán aquí cuando estén disponibles.' : `${tracks[currentIndex].artist} · ${tracks[currentIndex].album}`;
    content.querySelector('.track-list').hidden = true;
    content.querySelector('.up-next').hidden = true;
    button.addEventListener('blur', () => { heading.textContent = original; }, { once: true });
  }
}));
document.querySelectorAll('.tab').forEach(button => button.addEventListener('click', () => {
  if (button.dataset.tab === 'Lista') {
    document.querySelector('#track-list').hidden = false;
    document.querySelector('.up-next').hidden = false;
    document.querySelector('.album-heading h2').textContent = 'A Higher Place';
    document.querySelector('.album-heading span').textContent = 'Soundscape · 2024';
  }
}));

// Theme personalization: built-in palettes and a custom accent, persisted locally.
const themeStyle = document.createElement('style');
themeStyle.textContent = `
  body[data-player-theme="light"] { color:#172033; background:radial-gradient(ellipse at 50% 20%,#fff 0,#e8eef8 60%,#d1dcec 100%); }
  body[data-player-theme="light"] .window { background:#f4f7fc; border-color:#17203324; }
  body[data-player-theme="light"] .sidebar, body[data-player-theme="light"] .queue-panel { background:rgba(255,255,255,.92); }
  body[data-player-theme="light"] .main { background:linear-gradient(140deg,#f7f9fd,#e8eef8 55%,#dce6f3); }
  body[data-player-theme="light"] .nav-link, body[data-player-theme="light"] .browse-link, body[data-player-theme="light"] .recent-item,
  body[data-player-theme="light"] .track, body[data-player-theme="light"] .plain-control, body[data-player-theme="light"] .album-heading h2 { color:#25334a; }
  body[data-player-theme="light"] .search, body[data-player-theme="light"] .round-button, body[data-player-theme="light"] .add-button,
  body[data-player-theme="light"] .view-picker select { background:#ffffffb8; color:#25334a; }
  body[data-player-theme="light"] .desktop-footer { color:#40516c; }
  body[data-player-theme="light"] .track-details small, body[data-player-theme="light"] .album-heading span,
  body[data-player-theme="light"] .now-playing-info p { color:#66758c; }
  body[data-player-theme="light"] .tab { color:#53627a; }
  body[data-player-theme="light"] .tab.active { color:#172033; background:#17203312; }
  .theme-dialog-backdrop { position:fixed; inset:0; z-index:20; display:grid; place-items:center; padding:20px; background:#08111dcc; backdrop-filter:blur(8px); }
  .theme-dialog { width:min(440px,100%); padding:24px; color:#f2f6ff; background:#142238; border:1px solid #ffffff24; border-radius:16px; box-shadow:0 24px 80px #0008; font:14px Inter,"Segoe UI",sans-serif; }
  .theme-dialog h2 { margin:0 0 6px; font-size:20px; } .theme-dialog p { margin:0 0 18px; color:#a8b5c8; }
  .theme-options { display:grid; grid-template-columns:repeat(3,1fr); gap:10px; }
  .theme-option { padding:12px 8px; border:1px solid #ffffff24; border-radius:10px; color:inherit; background:#ffffff0b; cursor:pointer; font:inherit; }
  .theme-option[aria-pressed="true"] { border-color:#6dc6ff; box-shadow:0 0 0 2px #6dc6ff38; }
  .theme-swatch { display:block; width:100%; height:28px; margin-bottom:8px; border-radius:6px; background:linear-gradient(120deg,#101c2c 50%,#6dc6ff 50%); }
  .theme-option[data-choice="light"] .theme-swatch { background:linear-gradient(120deg,#edf2fa 50%,#557ee8 50%); }
  .theme-option[data-choice="purple"] .theme-swatch { background:linear-gradient(120deg,#21172e 50%,#c084fc 50%); }
  .theme-dialog label { display:flex; align-items:center; justify-content:space-between; margin-top:18px; padding-top:16px; border-top:1px solid #ffffff1a; }
  .theme-dialog input[type="color"] { width:46px; height:34px; padding:2px; border:1px solid #ffffff30; border-radius:7px; background:transparent; cursor:pointer; }
  .theme-dialog-footer { display:flex; justify-content:flex-end; margin-top:18px; }
  .theme-close { padding:8px 16px; border:0; border-radius:8px; color:#101c2c; background:#6dc6ff; cursor:pointer; font:600 13px inherit; }
`;
document.head.append(themeStyle);

const themeColors = { dark: '#6dc6ff', light: '#557ee8', purple: '#c084fc' };
function applyTheme(theme, accent) {
  const selectedTheme = ['dark', 'light', 'purple'].includes(theme) ? theme : 'dark';
  document.body.dataset.playerTheme = selectedTheme;
  document.documentElement.style.setProperty('--blue', accent || themeColors[selectedTheme]);
  document.documentElement.style.setProperty('--theme-accent', accent || themeColors[selectedTheme]);
  document.querySelectorAll('.theme-option').forEach(option => option.setAttribute('aria-pressed', String(option.dataset.choice === selectedTheme)));
  try { localStorage.setItem('wmp-theme-settings', JSON.stringify({ theme: selectedTheme, accent: accent || '' })); } catch (_) {}
}
try {
  const savedTheme = JSON.parse(localStorage.getItem('wmp-theme-settings') || '{}');
  applyTheme(savedTheme.theme || 'dark', savedTheme.accent || '');
} catch (_) { applyTheme('dark', ''); }

const settingsButton = document.querySelector('.round-button[aria-label="Configuración"]');
settingsButton.addEventListener('click', () => {
  if (document.querySelector('.theme-dialog-backdrop')) return;
  const saved = (() => { try { return JSON.parse(localStorage.getItem('wmp-theme-settings') || '{}'); } catch (_) { return {}; } })();
  const backdrop = document.createElement('div');
  backdrop.className = 'theme-dialog-backdrop';
  backdrop.innerHTML = `<section class="theme-dialog" role="dialog" aria-modal="true" aria-labelledby="theme-title"><h2 id="theme-title">Personalización de tema</h2><p>Elige el aspecto del reproductor y ajusta tu color de acento.</p><div class="theme-options"><button class="theme-option" data-choice="dark" aria-pressed="false"><span class="theme-swatch"></span>Oscuro</button><button class="theme-option" data-choice="light" aria-pressed="false"><span class="theme-swatch"></span>Claro</button><button class="theme-option" data-choice="purple" aria-pressed="false"><span class="theme-swatch"></span>Violeta</button></div><label for="theme-accent">Color de acento personalizado<input id="theme-accent" type="color" value="${saved.accent || themeColors[saved.theme] || themeColors.dark}"></label><div class="theme-dialog-footer"><button class="theme-close" type="button">Listo</button></div></section>`;
  document.body.append(backdrop);
  backdrop.querySelectorAll('.theme-option').forEach(option => option.addEventListener('click', () => {
    const colorPicker = backdrop.querySelector('#theme-accent');
    colorPicker.value = themeColors[option.dataset.choice];
    applyTheme(option.dataset.choice, colorPicker.value);
  }));
  backdrop.querySelector('#theme-accent').addEventListener('input', event => applyTheme(document.body.dataset.playerTheme, event.target.value));
  const closeDialog = () => backdrop.remove();
  backdrop.querySelector('.theme-close').addEventListener('click', closeDialog);
  backdrop.addEventListener('click', event => { if (event.target === backdrop) closeDialog(); });
  backdrop.addEventListener('keydown', event => { if (event.key === 'Escape') closeDialog(); });
  backdrop.querySelector('.theme-close').focus();
});

renderTrack(0);
