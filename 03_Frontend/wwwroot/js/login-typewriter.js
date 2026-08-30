/**
 * VisorDatosSIG 2026 - Login Typewriter & Animation Script
 * Control de 10 frases geoespaciales con 10s de visualización fija y tipografía Consolas
 */
(function() {
    const phrases = [
        "Geolocalización en Tiempo Real",
        "Análisis Espacial Avanzado",
        "Infraestructura de Datos Espaciales",
        "Gestión Catastral Inteligente",
        "Cartografía Digital Vectorial",
        "Georreferenciación WGS84",
        "Capas e Información Geoespacial",
        "Máxima Precisión Territorial",
        "Monitoreo Satelital Continuo",
        "Optimización de Rutas y Redes"
    ];

    let phraseIdx = 0;
    let charIdx = 0;
    let isDeleting = false;
    const typingSpeed = 80;
    const deletingSpeed = 35;
    const pauseAfterPhrase = 10000; // 10 segundos completos de visualización fija
    const pauseBeforeNext = 600;

    function runHandwrittenLoop() {
        const currentPhrase = phrases[phraseIdx];
        const textElem = document.getElementById('handwrittenPhrase');
        if (!textElem) return;

        if (isDeleting) {
            charIdx--;
            textElem.textContent = currentPhrase.substring(0, charIdx);
            if (charIdx === 0) {
                isDeleting = false;
                phraseIdx = (phraseIdx + 1) % phrases.length;
                setTimeout(runHandwrittenLoop, pauseBeforeNext);
                return;
            }
            setTimeout(runHandwrittenLoop, deletingSpeed);
        } else {
            charIdx++;
            textElem.textContent = currentPhrase.substring(0, charIdx);
            if (charIdx === currentPhrase.length) {
                isDeleting = true;
                setTimeout(runHandwrittenLoop, pauseAfterPhrase);
                return;
            }
            const jitter = Math.floor(Math.random() * 30) - 15;
            setTimeout(runHandwrittenLoop, typingSpeed + jitter);
        }
    }

    function initLottieSpeed() {
        const player = document.getElementById('sigLottiePlayer');
        if (player && typeof player.setSpeed === 'function') {
            player.setSpeed(0.5);
        }
    }

    let started = false;
    function start() {
        if (started) return;
        started = true;
        setTimeout(runHandwrittenLoop, 400);
        setTimeout(initLottieSpeed, 600);
    }

    document.addEventListener('DOMContentLoaded', start);

    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        start();
    }
})();
