window.ideaCanvas = {
    measureText: function (text) {
        const element = document.createElement("div");

        element.style.position = "absolute";
        element.style.visibility = "hidden";
        element.style.whiteSpace = "nowrap";

        element.textContent = text;

        document.body.appendChild(element);
        const rect = element.getBoundingClientRect();

        element.remove();

        return {
            width: rect.width,
            height: rect.height
        }

    }
}

window.ideaCanvas.focusElement = function(element) {
    if (element) element.focus();
};


window.ideaCanvas.svgToPngBase64 = function(containerElementId) {
    return new Promise((resolve, reject) => {
        const el = document.getElementById(containerElementId);
        if (!el) { reject('Element not found'); return; }

        if (typeof html2canvas === 'undefined') {
            reject('html2canvas ist nicht geladen');
            return;
        }

        html2canvas(el, {
            backgroundColor: '#0f2847',
            scale: 2
        }).then(function(canvas) {
            const dataUrl = canvas.toDataURL('image/png');
            resolve(dataUrl.substring(dataUrl.indexOf(',') + 1));
        }).catch(reject);
    });
};
