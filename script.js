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
  files.forEach(file => tracks.push({title: file.name.replace(/\.[^.]+$/, ''), artist: 'Tu biblioteca', album: file.type.startsWith('video/') ? 'Vídeos' : 'Archivos locales', duration: 0, file: URL.createObjectURL(file)}));
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
    button.addEventListener('blur', () => { heading.textContent = original; }, {once: true});
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
renderTrack(0);
