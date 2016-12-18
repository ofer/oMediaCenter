import {
    Component, OnChanges, HostBinding,
    trigger, transition, animate,
    style, state
} from '@angular/core';

import { SettingsService } from './omc.settings.service';

@Component({
    selector: 'omc-settings',
    templateUrl: 'omc.settings.component.html',
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

    public clientId: string;

    constructor(
        private settingsService: SettingsService) {
        settingsService.getClientId().then(clientId => this.clientId = clientId);
    }

    ngOnChanges(changes) {
    }


    setClientId() {
        this.settingsService.setClientId(this.clientId);
    }
}


/*
Copyright 2016 Google Inc. All Rights Reserved.
Use of this source code is governed by an MIT-style license that
can be found in the LICENSE file at http://angular.io/license
*/