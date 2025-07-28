import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
    selector: 'app-media-list-page',
    templateUrl: './media-list-page.component.html',
    styleUrls: ['./media-list-page.component.css'],
    standalone: false
})
export class MediaListPageComponent {

  constructor(private router: Router) {}

  mediaSelected(hash: string) {
    this.router.navigate(['/media', hash]);
  }
}
