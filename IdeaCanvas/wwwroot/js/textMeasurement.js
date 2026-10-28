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


window.ideaCanvas.svgToPngBase64 = function (containerElementId) {
    return new Promise((resolve, reject) => {
        const containerElement = document.getElementById(containerElementId);
        if (!containerElement) {
            reject('Element not found');
            return;
        }

        if (typeof html2canvas === 'undefined') {
            reject('html2canvas ist nicht geladen');
            return;
        }

        html2canvas(containerElement, {
            backgroundColor: '#e8eff7',
            scale: 2,

            // Pins und Strichellinien nur im Export ausblenden
            onclone: function (clonedDocument) {
    const clonedContainer = clonedDocument.getElementById(containerElementId);
    if (!clonedContainer) {
        return;
    }

    // Pin-Punkte komplett aus der Kopie entfernen
    const pinIcons = clonedContainer.querySelectorAll('.pin-icon');
    pinIcons.forEach(function (pinIcon) {
        pinIcon.remove();
    });

    // Gepinnte Nodes bekommen den normalen Rahmen
    const pinnedRects = clonedContainer.querySelectorAll('.node-rect.is-pinned');
    pinnedRects.forEach(function (pinnedRect) {
        pinnedRect.style.stroke = 'rgba(47, 93, 138, 0.35)';
        pinnedRect.style.strokeWidth = '1.5';
        pinnedRect.style.strokeDasharray = 'none';
    });
}
        }).then(function (canvas) {
            const dataUrl = canvas.toDataURL('image/png');
            resolve(dataUrl.substring(dataUrl.indexOf(',') + 1));
        }).catch(reject);
    });
};
