window.ideaCanvas = window.ideaCanvas || {};

window.ideaCanvas.resizeObservers = {};

// Meldet die Größe des Elements an C#, sofort und bei jeder Änderung
window.ideaCanvas.observeSize = function (elementId, dotNetReference) {
    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }

    const observer = new ResizeObserver(function (entries) {
        const size = entries[0].contentRect;
        dotNetReference.invokeMethodAsync("OnCanvasResized", size.width, size.height);
    });

    observer.observe(element);
    window.ideaCanvas.resizeObservers[elementId] = observer;
};

// Beendet die Beobachtung, wenn der Canvas verschwindet
window.ideaCanvas.stopObservingSize = function (elementId) {
    const observer = window.ideaCanvas.resizeObservers[elementId];
    if (observer) {
        observer.disconnect();
        delete window.ideaCanvas.resizeObservers[elementId];
    }
};