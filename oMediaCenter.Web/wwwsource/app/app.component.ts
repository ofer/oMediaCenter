import { Component } from '@angular/core';
import { ClientControlService } from './client-control.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'oMediaCenter';

  constructor(clientService: ClientControlService) {
    clientService.startPolling();
  }
}
