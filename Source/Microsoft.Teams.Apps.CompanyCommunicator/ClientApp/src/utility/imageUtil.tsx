import ColorHash from "color-hash";

export class ImageUtil {

    public static makeInitialImage = (name: string) => {
        var canvas = document.createElement('canvas');
        canvas.style.display = 'none';
        canvas.width = 32;
        canvas.height = 32;
        document.body.appendChild(canvas);
        var context = canvas.getContext('2d');
        if (context) {
            let colorHash = new ColorHash();
            var colorNum = colorHash.hex(name);
            context.fillStyle = colorNum;
            context.fillRect(0, 0, canvas.width, canvas.height);
            context.font = "16px Arial";
            context.fillStyle = "#fff";
            var split = name.split(' ');
            var len = split.length;
            var first = split[0][0];
            var last = null;
            if (len > 1) {
                last = split[len - 1][0];
            }
            if (last) {
                var initials = first + last;
                context.fillText(initials.toUpperCase(), 3, 23);
            } else {
                var initials = first;
                context.fillText(initials.toUpperCase(), 10, 23);
            }
            var data = canvas.toDataURL();
            document.body.removeChild(canvas);
            return data;
        } else {
            return "";
        }
    }
}