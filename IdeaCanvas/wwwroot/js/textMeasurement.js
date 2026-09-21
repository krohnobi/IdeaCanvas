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

