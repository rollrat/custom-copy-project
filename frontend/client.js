// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.
// Custom Copy Frontend Client UI Source Code

const fs = require("fs");
const WebSocket = require("ws");
const { app, dialog } = require("electron");

if (!fs.existsSync("settings.json"))
{
  process.exit();
}

let settings_json = fs.readFileSync("settings.json");
let settings = JSON.parse(settings_json);

var ws = new WebSocket(settings.ConnectionAddress, {
  perMessageDeflate: false,
});

ws.on("error", (err) => {
  dialog.showMessageBoxSync(null, {
    title: "Custom Copy",
    type: "error",
    message:
      "Cannot find websocket server!\nPlease run custom-copy-server before running custom-copy-frontend-client-ui!" +
      "\nRaw: " +
      err,
  });
  app.quit();
});

ws.on("open", function open() {
  ws.send("hello");
});

ws.on("message", (msg) => {
  if (msg == "ping")
    ws.send("pong");
});

module.exports.ws = ws;
