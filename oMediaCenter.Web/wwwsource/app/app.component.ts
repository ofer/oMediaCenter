import { Component } from '@angular/core';
import { ClientControlService } from './client-control.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'oMediaCenter';

  lastMouseX: number | null = null;
  lastMouseY: number | null = null;

  diagnosticText: string = "";

  constructor(clientService: ClientControlService) {
    clientService.startPolling();
    // detect this is a smart tv
    var userAgent = window.navigator.userAgent;
    // if (userAgent.includes("SMART-TV") && userAgent.includes("Tizen")) {
    if (false) {
    // if (true) {
      // do something for smart tv
      console.log("Running on a Smart TV");
      // capture mouse move events and convert them to key events
      document.addEventListener('mousemove', (event) => {
        if (this.lastMouseX === null || this.lastMouseY === null) {
          this.lastMouseX = event.clientX;
          this.lastMouseY = event.clientY;
          return;
        }

        const deltaX = event.clientX - this.lastMouseX;
        const deltaY = event.clientY - this.lastMouseY;

        if (Math.abs(deltaX) > Math.abs(deltaY)) {
          if (deltaX > 0) {
            this.simulateKeyEvent('ArrowRight');
          } else {
            this.simulateKeyEvent('ArrowLeft');
          }
        } else {
          if (deltaY > 0) {
            this.simulateKeyEvent('ArrowDown');
          } else {
            this.simulateKeyEvent('ArrowUp');
          }
        }

        this.lastMouseX = event.clientX;
        this.lastMouseY = event.clientY;
      });

      document.addEventListener('click', (event) => {
        // Simulate Enter key on click
        this.simulateKeyEvent('Enter');
        event.preventDefault();
      });
    } else {
      // do something for non-smart tv
      console.log("Running on a non-Smart TV");
    }
  }

  keyPressCount: number = 0;

  simulateKeyEvent(key: string) {
    const event = new KeyboardEvent('keydown', { key: key });
    document.dispatchEvent(event);
    this.diagnosticText = `${key} ${this.keyPressCount}`;
    console.log(`Simulated key event: ${key}`);
    this.keyPressCount++;
  }
}
