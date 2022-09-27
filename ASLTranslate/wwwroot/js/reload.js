export function ReloadVideo() {
    var video = document.getElementById('avatar-video');
    video.load();
    video.play();
}