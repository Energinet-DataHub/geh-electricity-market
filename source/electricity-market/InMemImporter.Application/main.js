const { app, BrowserWindow, ipcMain } = require("electron");
const { exec } = require("child_process");
const { randomUUID } = require("crypto");
const path = require("path");
const os = require("os");
const fs = require("fs");

const findGitRoot = (startPath) => {
  let currentPath = path.resolve(startPath);

  while (currentPath !== path.parse(currentPath).root) {
    const gitPath = path.join(currentPath, ".git");

    if (fs.existsSync(gitPath) && fs.statSync(gitPath).isDirectory()) {
      return currentPath;
    }

    currentPath = path.dirname(currentPath);
  }

  return null;
};

const gitRoot = findGitRoot(path.dirname(process.execPath));
const logFilePath = path.join(gitRoot, "tools", "inmemimporter.log");

const logToFile = (message) => {
  if (!fs.existsSync(logFilePath)) {
    return;
  }
  const logEntry = `${new Date().toISOString()} - ${message}\n`;
  fs.appendFileSync(logFilePath, logEntry);
};

const tempDir = path.join(os.tmpdir(), "3aabcc4f-1153-46d3-b6a3-b503985985fd");

if (!fs.existsSync(tempDir)) {
  fs.mkdirSync(tempDir);
  logToFile(`Created temp directory: ${tempDir}`);
}

const createCsvFile = (inputString) => {
  const p = path.join(tempDir, randomUUID() + ".csv");
  fs.writeFileSync(p, inputString, "utf-8");
  logToFile(`Created CSV file: ${p}`);
  return p;
};

const createWindow = () => {
  const win = new BrowserWindow({
    width: 1600,
    height: 1200,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
    },
  });

  win.loadFile("index.html");
};

app.whenReady().then(() => {
  createWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});

app.on("will-quit", () => {
  try {
    fs.rmSync(tempDir, { recursive: true, force: true });
    logToFile(`Deleted temp directory: ${tempDir}`);
  } catch (err) {
    logToFile(`Failed to delete temp directory: ${tempDir}`);
  }
});

let running = false;

ipcMain.handle("run-console-app", async (_, inputString) => {
  if (running) {
    return "Another export is already running, please wait for it to finish.";
  }

  running = true;

  return new Promise((resolve) => {
    const filePath = createCsvFile(inputString);
    const command = `dotnet run --project ${gitRoot}/source/electricity-market/InMemImporter/InMemImporter.csproj ${filePath}`;

    exec(command, (error, stdout, stderr) => {
      running = false;
      if (error) {
        logToFile("error: " + error.message);
      }
      if (stderr) {
        logToFile("stderr: " + stderr);
      }
      if (stdout) {
        resolve(stdout);
      }
    });
  });
});
