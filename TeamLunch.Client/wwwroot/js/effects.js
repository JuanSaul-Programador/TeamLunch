// TeamLunch Premium Effects
window.teamLunchEffects = {
    // Confetti using canvas-confetti library (loaded via CDN)
    fireConfetti: function () {
        if (typeof confetti === 'function') {
            // Big celebration burst
            confetti({
                particleCount: 150,
                spread: 70,
                origin: { y: 0.6 }
            });

            // Side cannons
            setTimeout(() => {
                confetti({
                    particleCount: 50,
                    angle: 60,
                    spread: 55,
                    origin: { x: 0 }
                });
                confetti({
                    particleCount: 50,
                    angle: 120,
                    spread: 55,
                    origin: { x: 1 }
                });
            }, 250);
        }
    },

    // Sound effects
    playSound: function (soundType) {
        const sounds = {
            vote: 'https://cdn.freesound.org/previews/341/341695_5858296-lq.mp3',
            join: 'https://cdn.freesound.org/previews/320/320655_5260872-lq.mp3',
            winner: 'https://cdn.freesound.org/previews/270/270402_5123851-lq.mp3'
        };

        const url = sounds[soundType];
        if (url) {
            const audio = new Audio(url);
            audio.volume = 0.3;
            audio.play().catch(() => { }); // Ignore autoplay restrictions
        }
    },

    // Request notification permission
    requestNotificationPermission: async function () {
        if ('Notification' in window) {
            const permission = await Notification.requestPermission();
            return permission === 'granted';
        }
        return false;
    },

    // Show browser notification
    showNotification: function (title, body) {
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(title, {
                body: body,
                icon: '/favicon.ico',
                badge: '/favicon.ico'
            });
        }
    },

    // Audio Recording
    mediaRecorder: null,
    audioChunks: [],

    startRecording: async function () {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            this.mediaRecorder = new MediaRecorder(stream);
            this.audioChunks = [];

            this.mediaRecorder.ondataavailable = (event) => {
                this.audioChunks.push(event.data);
            };

            this.mediaRecorder.start();
            return true;
        } catch (err) {
            console.error("Error accessing microphone:", err);
            return false;
        }
    },

    stopRecording: function () {
        return new Promise((resolve) => {
            if (!this.mediaRecorder) return resolve(null);

            this.mediaRecorder.onstop = () => {
                const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
                const reader = new FileReader();
                reader.readAsDataURL(audioBlob);
                reader.onloadend = () => {
                    resolve(reader.result); // Returns Base64 string
                };
            };

            this.mediaRecorder.stop();
            // Stop all tracks to release microphone
            this.mediaRecorder.stream.getTracks().forEach(track => track.stop());
            this.mediaRecorder = null;
        });
    }
};
