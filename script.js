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
  seek.max = track.duration || 1;
  seek.value = 0;
  elapsed.textContent = '0:00';
  document.querySelectorAll('.track').forEach((row, i) => {
    row.classList.toggle('selected', i === currentIndex);
    const number = row.querySelector('.track-number');
    if (number) number.textContent = i === currentIndex ? '▮' : String(i + 1);
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
  if (!tracks[currentIndex].file) return document.querySelector('#file-picker').click();
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
  const listTab = button.dataset.tab === 'Lista';
  document.querySelector('#track-list').hidden = !listTab;
  document.querySelector('.up-next').hidden = !listTab;
  const heading = document.querySelector('.album-heading h2');
  const subtitle = document.querySelector('.album-heading span');
  if (listTab) { heading.textContent = 'A Higher Place'; subtitle.textContent = 'Soundscape · 2024'; }
  else {
    heading.textContent = button.dataset.tab === 'Letras' ? 'Letras no disponibles' : 'Información de la pista';
    subtitle.textContent = button.dataset.tab === 'Letras' ? 'Las letras aparecerán aquí cuando estén disponibles.' : `${tracks[currentIndex].artist} · ${tracks[currentIndex].album}`;
  }
}));

// Temas, color de acento y controles de volumen.
const palettes = {
  dark: { label: 'Oscuro', accent: '#6dc6ff', background: '#101c2c', panel: '#142238' },
  light: { label: 'Claro', accent: '#557ee8', background: '#e8eef8', panel: '#f4f7fc' },
  purple: { label: 'Violeta', accent: '#c084fc', background: '#21172e', panel: '#30203f' },
  green: { label: 'Verde', accent: '#4ade80', background: '#10251e', panel: '#19372a' },
  rose: { label: 'Rosa', accent: '#fb7185', background: '#2b1721', panel: '#40212f' },
  sunset: { label: 'Atardecer', accent: '#fb923c', background: '#2a1c24', panel: '#422831' },
  ocean: { label: 'Océano', accent: '#22d3ee', background: '#10232d', panel: '#193440' }
};
let themeSettings = { theme: 'dark', accent: '' };
try { themeSettings = { ...themeSettings, ...JSON.parse(localStorage.getItem('wmp-theme-settings') || '{}') }; } catch (_) {}
const uiStyle = document.createElement('style');
uiStyle.textContent = `
  :root { --theme-accent: #6dc6ff; }
  body, .window, .sidebar, .main, .queue-panel, .theme-dialog, .theme-dialog-backdrop, .round-button, .search, .add-button, .view-picker select, .track, .tab, .plain-control, .player-controls, .desktop-footer { transition: background-color .35s ease, color .35s ease, border-color .35s ease, box-shadow .35s ease; }
  body.theme-changing .window { animation: theme-arrive .42s ease both; }
  @keyframes theme-arrive { 0% { opacity:.78; transform:scale(.992); filter:saturate(.7); } 100% { opacity:1; transform:scale(1); filter:saturate(1); } }
  body[data-player-theme="light"] { color:#172033; background:radial-gradient(ellipse at 50% 20%,#fff 0,#e8eef8 60%,#d1dcec 100%); }
  body[data-player-theme="light"] .window { background:#f4f7fc; border-color:#17203324; }
  body[data-player-theme="light"] .sidebar, body[data-player-theme="light"] .queue-panel { background:rgba(255,255,255,.92); }
  body[data-player-theme="light"] .main { background:linear-gradient(140deg,#f7f9fd,#e8eef8 55%,#dce6f3); }
  body[data-player-theme="light"] .nav-link, body[data-player-theme="light"] .browse-link, body[data-player-theme="light"] .recent-item, body[data-player-theme="light"] .track, body[data-player-theme="light"] .plain-control, body[data-player-theme="light"] .album-heading h2 { color:#25334a; }
  body[data-player-theme="light"] .search, body[data-player-theme="light"] .round-button, body[data-player-theme="light"] .add-button, body[data-player-theme="light"] .view-picker select { background:#ffffffb8; color:#25334a; }
  body[data-player-theme="light"] .desktop-footer { color:#40516c; }
  body[data-player-theme="light"] .track-details small, body[data-player-theme="light"] .album-heading span, body[data-player-theme="light"] .now-playing-info p { color:#66758c; }
  body[data-player-theme="light"] .tab { color:#53627a; } body[data-player-theme="light"] .tab.active { color:#172033; background:#17203312; }
  body[data-player-theme="purple"] .window { background:#21172e; } body[data-player-theme="purple"] .sidebar, body[data-player-theme="purple"] .queue-panel { background:#30203fee; } body[data-player-theme="purple"] .main { background:linear-gradient(140deg,#352044,#21172e 60%,#1d172b); }
  body[data-player-theme="green"] .window { background:#10251e; } body[data-player-theme="green"] .sidebar, body[data-player-theme="green"] .queue-panel { background:#19372aee; } body[data-player-theme="green"] .main { background:linear-gradient(140deg,#1d4431,#10251e 60%,#0d201b); }
  body[data-player-theme="rose"] .window { background:#2b1721; } body[data-player-theme="rose"] .sidebar, body[data-player-theme="rose"] .queue-panel { background:#40212fee; } body[data-player-theme="rose"] .main { background:linear-gradient(140deg,#4a2636,#2b1721 60%,#24151f); }
  body[data-player-theme="sunset"] .window { background:#2a1c24; } body[data-player-theme="sunset"] .sidebar, body[data-player-theme="sunset"] .queue-panel { background:#422831ee; } body[data-player-theme="sunset"] .main { background:linear-gradient(140deg,#573329,#2a1c24 60%,#241922); }
  body[data-player-theme="ocean"] .window { background:#10232d; } body[data-player-theme="ocean"] .sidebar, body[data-player-theme="ocean"] .queue-panel { background:#193440ee; } body[data-player-theme="ocean"] .main { background:linear-gradient(140deg,#1c4552,#10232d 60%,#0e2029); }
  .player-controls .volume-controls { display:flex; align-items:center; gap:8px; margin-left:12px; }
  .volume-toggle { border:0; background:transparent; color:inherit; font-size:17px; cursor:pointer; padding:5px; }
  .volume-slider { width:82px; height:4px; accent-color:var(--theme-accent); cursor:pointer; }
  .theme-launch { border:1px solid #ffffff24; border-radius:18px; padding:7px 11px; color:inherit; background:#ffffff0b; cursor:pointer; font:inherit; white-space:nowrap; }
  .theme-dialog-backdrop { position:fixed; inset:0; z-index:30; display:grid; place-items:center; padding:20px; background:#08111dcc; backdrop-filter:blur(8px); animation:fade-in .2s ease both; }
  .theme-dialog { width:min(520px,100%); max-height:90vh; overflow:auto; padding:24px; color:#f2f6ff; background:#142238; border:1px solid #ffffff24; border-radius:16px; box-shadow:0 24px 80px #0008; font:14px Inter,"Segoe UI",sans-serif; animation:dialog-in .25s ease both; }
  @keyframes fade-in { from { opacity:0; } to { opacity:1; } } @keyframes dialog-in { from { opacity:0; transform:translateY(12px) scale(.98); } to { opacity:1; transform:translateY(0) scale(1); } }
  .theme-dialog h2 { margin:0 0 6px; font-size:20px; } .theme-dialog p { margin:0 0 18px; color:#a8b5c8; }
  .theme-options { display:grid; grid-template-columns:repeat(4,1fr); gap:9px; }
  .theme-option { padding:10px 6px; border:1px solid #ffffff24; border-radius:10px; color:inherit; background:#ffffff0b; cursor:pointer; font:inherit; }
  .theme-option[aria-pressed="true"] { border-color:var(--theme-accent); box-shadow:0 0 0 2px color-mix(in srgb,var(--theme-accent) 30%,transparent); }
  .theme-swatch { display:block; width:100%; height:24px; margin-bottom:7px; border-radius:6px; }
  .theme-dialog label { display:flex; align-items:center; justify-content:space-between; margin-top:18px; padding-top:16px; border-top:1px solid #ffffff1a; }
  .theme-dialog input[type="color"] { width:46px; height:34px; padding:2px; border:1px solid #ffffff30; border-radius:7px; background:transparent; cursor:pointer; }
  .theme-dialog-footer { display:flex; justify-content:flex-end; margin-top:18px; }
  .theme-close { padding:8px 16px; border:0; border-radius:8px; color:#101c2c; background:var(--theme-accent); cursor:pointer; font:600 13px Inter,"Segoe UI",sans-serif; }
  @media(max-width:560px) { .theme-options { grid-template-columns:repeat(2,1fr); } .volume-slider { width:58px; } .player-controls .volume-controls { margin-left:2px; } }
  @media(prefers-reduced-motion:reduce) { *,*::before,*::after { animation-duration:.01ms!important; transition-duration:.01ms!important; } }
`;
document.head.append(uiStyle);
function saveTheme() { try { localStorage.setItem('wmp-theme-settings', JSON.stringify(themeSettings)); } catch (_) {} }
function applyTheme(animate = true) {
  const key = palettes[themeSettings.theme] ? themeSettings.theme : 'dark';
  themeSettings.theme = key;
  const palette = palettes[key];
  document.body.dataset.playerTheme = key;
  document.documentElement.style.setProperty('--blue', themeSettings.accent || palette.accent);
  document.documentElement.style.setProperty('--theme-accent', themeSettings.accent || palette.accent);
  if (animate) {
    document.body.classList.remove('theme-changing');
    void document.body.offsetWidth;
    document.body.classList.add('theme-changing');
    window.setTimeout(() => document.body.classList.remove('theme-changing'), 450);
  }
  document.querySelectorAll('.theme-option').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.choice === key)));
  saveTheme();
}
applyTheme(false);

const themeButton = document.createElement('button');
themeButton.className = 'theme-launch'; themeButton.type = 'button'; themeButton.textContent = '🎨 Tema'; themeButton.setAttribute('aria-label', 'Personalizar tema');
const settingsButton = document.querySelector('.round-button[aria-label="Configuración"]');
settingsButton.insertAdjacentElement('beforebegin', themeButton);
const volumeControls = document.createElement('div');
volumeControls.className = 'volume-controls';
volumeControls.innerHTML = '<button class="volume-toggle" type="button" aria-label="Silenciar" title="Silenciar">🔊</button><input class="volume-slider" type="range" min="0" max="1" step="0.01" value="1" aria-label="Volumen">';
document.querySelector('.player-controls').append(volumeControls);
const volumeButton = volumeControls.querySelector('.volume-toggle');
const volumeSlider = volumeControls.querySelector('.volume-slider');
let savedVolume = 1;
try { savedVolume = Number(localStorage.getItem('wmp-volume')); } catch (_) {}
if (!Number.isFinite(savedVolume) || savedVolume < 0 || savedVolume > 1) savedVolume = 1;
audio.volume = savedVolume; volumeSlider.value = String(savedVolume);
function updateVolumeButton() {
  const muted = audio.muted || audio.volume === 0;
  volumeButton.textContent = muted ? '🔇' : audio.volume < .5 ? '🔉' : '🔊';
  volumeButton.setAttribute('aria-label', muted ? 'Activar sonido' : 'Silenciar');
  volumeButton.title = muted ? 'Activar sonido' : 'Silenciar';
}
volumeButton.addEventListener('click', () => { audio.muted = !audio.muted; updateVolumeButton(); });
volumeSlider.addEventListener('input', () => {
  audio.volume = Number(volumeSlider.value); audio.muted = audio.volume === 0;
  try { localStorage.setItem('wmp-volume', String(audio.volume)); } catch (_) {}
  updateVolumeButton();
});
updateVolumeButton();

function openThemeDialog() {
  if (document.querySelector('.theme-dialog-backdrop')) return;
  const backdrop = document.createElement('div'); backdrop.className = 'theme-dialog-backdrop';
  const options = Object.entries(palettes).map(([key, palette]) => `<button class="theme-option" type="button" data-choice="${key}" aria-pressed="false"><span class="theme-swatch" style="background:linear-gradient(120deg,${palette.background} 50%,${palette.accent} 50%)"></span>${palette.label}</button>`).join('');
  backdrop.innerHTML = `<section class="theme-dialog" role="dialog" aria-modal="true" aria-labelledby="theme-title"><h2 id="theme-title">Personalización de tema</h2><p>Elige una paleta y, si quieres, define tu propio color de acento.</p><div class="theme-options">${options}</div><label for="theme-accent">Color de acento personalizado<input id="theme-accent" type="color" value="${themeSettings.accent || palettes[themeSettings.theme].accent}"></label><div class="theme-dialog-footer"><button class="theme-close" type="button">Listo</button></div></section>`;
  document.body.append(backdrop);
  backdrop.querySelectorAll('.theme-option').forEach(button => button.addEventListener('click', () => {
    themeSettings.theme = button.dataset.choice; themeSettings.accent = '';
    backdrop.querySelector('#theme-accent').value = palettes[themeSettings.theme].accent;
    applyTheme();
  }));
  backdrop.querySelector('#theme-accent').addEventListener('input', event => { themeSettings.accent = event.target.value; applyTheme(); });
  const close = () => backdrop.remove();
  backdrop.querySelector('.theme-close').addEventListener('click', close);
  backdrop.addEventListener('click', event => { if (event.target === backdrop) close(); });
  backdrop.addEventListener('keydown', event => { if (event.key === 'Escape') close(); });
  backdrop.querySelector('.theme-close').focus();
}
themeButton.addEventListener('click', openThemeDialog);
settingsButton.addEventListener('click', openThemeDialog);
renderTrack(0);
