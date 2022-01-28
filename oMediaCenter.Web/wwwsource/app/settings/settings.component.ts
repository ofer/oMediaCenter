import { Component, OnInit, HostBinding, OnChanges } from '@angular/core';
import { SettingsService } from '../settings.service';
import { trigger, state, style, transition, animate } from '@angular/animations';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css'],
  animations: [
    trigger('routeAnimation', [
        state('*',
            style({
                opacity: 1,
                transform: 'translateX(0)'
            })
        ),
        transition('void => *', [
            style({
                opacity: 0,
                transform: 'translateX(-100%)'
            }),
            animate('0.2s ease-in')
        ]),
        transition('* => void', [
            animate('0.5s ease-out', style({
                opacity: 0,
                transform: 'translateY(100%)'
            }))
        ])
    ])
  ]
})
export class SettingsComponent implements OnChanges {

  @HostBinding('@routeAnimation') get routeAnimation() {
    return true;
  }

  @HostBinding('style.display') get display() {
      return 'block';
  }

  @HostBinding('style.position') get position() {
      return 'absolute';
  }

  public clientId!: string;

  constructor(
      private settingsService: SettingsService) {
        this.clientId = settingsService.getClientId() as string;
  }

  ngOnChanges(changes: any) {
  }


  setClientId() {
      this.settingsService.setClientId(this.clientId);
  }
}
