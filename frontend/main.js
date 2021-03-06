// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.
// Custom Copy Frontend Client UI Source Code

const { app, BrowserWindow } = require("electron");
const client = require("./client.js");
 
function createWindow() {
  let win = new BrowserWindow({
    width: 800,
    height: 600,
    frame: false,
    webPreferences: {
      nodeIntegration: true,
    },
  });

  win.loadFile("index.html");
  win.setMenu(null);

  //win.webContents.openDevTools();
}

app.whenReady().then(createWindow);

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
    client.ws.close();
  }
});

app.on("activate", () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});
