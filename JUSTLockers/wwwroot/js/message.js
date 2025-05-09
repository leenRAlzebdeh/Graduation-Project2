setTimeout(function () {
    const msgBox = document.querySelector('.message-box');
    if (msgBox) {
        msgBox.classList.add('fade-out');
        setTimeout(() => msgBox.style.display = 'none', 600); // match fade duration
    }
}, 4000); // 4 second delay before fade