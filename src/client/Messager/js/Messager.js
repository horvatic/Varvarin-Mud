const socket = new WebSocket('ws://192.168.0.10:49890');

var btn = document.getElementById("submit");
btn.onclick = function() {
    var message = document.getElementById("message").value;
    socket.send(message);
  }

socket.addEventListener('message', function (event) {
    var para = document.createElement("p");
    var message = document.createTextNode(event.data);
    para.appendChild(message);
    var element = document.getElementById("MessageArea");
    element.appendChild(para);
});