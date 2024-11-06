const { app, BrowserWindow, dialog } = require('electron');
const path = require('path');
const { exec } = require('child_process');

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require('electron-squirrel-startup')) {
  app.quit();
}

function checkDockerInstallation(callback) {
  exec('docker --version', (error) => {
    if (error) {
      console.log('Docker is not installed or not in PATH');
      dialog.showMessageBox({
        type: 'warning',
        title: 'Docker Not Found',
        message: 'Docker is required but not installed or not in PATH.',
        buttons: ['Download Docker', 'Cancel'],
        defaultId: 0,
        cancelId: 1
      }).then(result => {
        if (result.response === 0) {
          require('electron').shell.openExternal('https://www.docker.com/products/docker-desktop');
        }
      });
      callback(false);
    } else {
      console.log('Docker is installed');
      callback(true);
    }
  });
}

const createWindow = () => {
  // Create the browser window.
  const mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
    },
  });

  checkDockerInstallation((isInstalled) => {
    if (isInstalled) {
      // Start Docker Compose
      exec('docker-compose up --build -d', { cwd: path.join(__dirname, '..', '..') }, (error, stdout, stderr) => {
        if (error) {
          console.error(`exec error: ${error}`);
          return;
        }
        console.log(`Docker Compose output: ${stdout}`);
        if (stderr) console.error(`Docker Compose errors: ${stderr}`);

        // Load the index.html of the app after Docker Compose is up
        mainWindow.loadFile(path.join(__dirname, 'index.html'));
      });
    } else {
      mainWindow.loadFile(path.join(__dirname, 'docker-required.html'));
    }
  });

  // Open the DevTools.
  mainWindow.webContents.openDevTools();

  // Emitted when the window is closed.
  mainWindow.on('closed', () => {
    // Stop Docker Compose
    exec('docker-compose down', { cwd: path.join(__dirname, '..', '..') }, (error, stdout, stderr) => {
      if (error) {
        console.error(`exec error: ${error}`);
        return;
      }
      console.log(`Docker Compose shutdown output: ${stdout}`);
      if (stderr) console.error(`Docker Compose shutdown errors: ${stderr}`);
    });
  });
};

// This method will be called when Electron has finished initialization
// and is ready to create browser windows.
app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});