// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.
// Koromo Copy Frontend Client UI Source Code

const fs = require("fs");
const WebSocket = require("ws");
const { app, dialog } = require("electron");

if (!fs.existsSync("settings.json"))
{
  dialog.showMessageBoxSync(null, {
    title: "Koromo Copy",
    type: "error",
    message:
      "Setting file not found!\nPlease run koromo-copy-server before running koromo-copy-frontend-client-ui!"
  });
  app.quit();
}

let settings_json = fs.readFileSync("settings.json");
let settings = JSON.parse(settings_json);

var ws = new WebSocket(settings.ConnectionAddress, {
  perMessageDeflate: false,
});

ws.on("error", (err) => {
  dialog.showMessageBoxSync(null, {
    title: "Koromo Copy",
    type: "error",
    message:
      "Cannot find websocket server!\nPlease run koromo-copy-server before running koromo-copy-frontend-client-ui!" +
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
