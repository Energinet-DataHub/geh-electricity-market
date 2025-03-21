const { app, BrowserWindow, ipcMain, dialog } = require("electron");
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

const determineDelimiter = (inputString) => {
  return inputString.indexOf(";") < inputString.indexOf("\n") ? ";" : ",";
};

const createCsvFile = (delimiter, inputString) => {
  const tempFilePath = path.join(tempDir, randomUUID() + ".csv");
  fs.writeFileSync(tempFilePath, inputString, "utf-8");
  logToFile(`Created CSV file: ${tempFilePath}`);
  return tempFilePath;
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

ipcMain.handle("run-import", async (_, inputString) => {
  if (running) {
    return "Another action is already running, please wait for it to finish.";
  }

  running = true;

  return new Promise((resolve) => {
    const delimiter = determineDelimiter(inputString);
    const filePath = createCsvFile(delimiter, inputString);
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
        resolve(
          stdout
            .split("\n")
            .filter((line) => !line.includes(": warning :"))
            .join("\n")
        );
      }
    });
  });
});

ipcMain.handle("create-scenario", (_, csv, importResult) => {
  if (running) {
    return "Another action is already running, please wait for it to finish.";
  }

  if (!importResult.includes("+-")) {
    return "Please make an import first.";
  }

  running = true;

  return new Promise((resolve) => {
    dialog
      .showSaveDialog({
        title: "Save As",
        defaultPath: path.join(
          gitRoot,
          "source",
          "electricity-market",
          "ElectricityMarket.IntegrationTests",
          "TestData",
          "SCENARIO_NAME.csv"
        ),
      })
      .then((result) => {
        running = false;
        if (!result.canceled && result.filePath) {
          fs.writeFile(result.filePath, csv, (err) => {
            if (err) {
              logToFile(err);
              resolve("Failed to save scenario.");
              return;
            }
            fs.writeFile(
              result.filePath.replace(".csv", "") + ".txt",
              importResult,
              (err) => {
                if (err) {
                  logToFile(err);
                  resolve("Failed to save scenario.");
                  return;
                }
                resolve("Scenario saved successfully.");
              }
            );
          });
        }
      });
  });
});

ipcMain.handle("run-scenarios", () => {
  if (running) {
    return "Another action is already running, please wait for it to finish.";
  }

  running = true;

  return new Promise((resolve) => {
    const sep = process.platform === "win32" ? "&" : "&&";
    const command = `cd ${path.join(
      gitRoot,
      "source",
      "electricity-market",
      "ElectricityMarket.IntegrationTests"
    )} ${sep} dotnet build --no-incremental ${sep} dotnet test --logger "console;verbosity=detailed" --filter "Energinet.DataHub.ElectricityMarket.IntegrationTests.Scenarios.ScenarioTests.Test_Scenario"`;

    exec(command, (error, stdout, stderr) => {
      running = false;
      if (error) {
        logToFile("error: " + error.message);
      }
      if (stderr) {
        logToFile("stderr: " + stderr);
      }
      if (stdout) {
        resolve(
          "Results:\n" +
            stdout
              .split("\n")
              .filter(
                (line) =>
                  line.includes("Passed") ||
                  line.includes("Failed") ||
                  line.includes("Total tests") ||
                  line.includes("Total time")
              )
              .join("\n")
        );
      }
    });
  });
});

ipcMain.handle("delete-scenario", () => {
  if (running) {
    return "Another action is already running, please wait for it to finish.";
  }

  running = true;

  return new Promise((resolve) => {
    dialog
      .showOpenDialog({
        title: "Select scenario to delete",
        defaultPath: path.join(
          gitRoot,
          "source",
          "electricity-market",
          "ElectricityMarket.IntegrationTests",
          "TestData"
        ),
        filters: [{ name: "CSV Files", extensions: ["csv"] }],
        properties: ["openFile"],
      })
      .then((result) => {
        running = false;
        if (!result.canceled && result.filePaths.length) {
          const file = result.filePaths[0];
          fs.unlink(file, (err) => {
            if (err) {
              logToFile(err);
              resolve("Failed to delete scenario.");
              return;
            }
            fs.unlink(file.replace(".csv", "") + ".txt", (err) => {
              if (err) {
                logToFile(err);
                resolve("Failed to delete scenario.");
                return;
              }
              resolve("Scenario deleted successfully");
            });
          });
        }
      });
  });
});
