import { Component } from '@angular/core';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.css',
    imports: [NavMenuComponent, RouterOutlet, RouterLink, RouterLinkActive]
})
export class AppComponent {

  title = 'Angular.Client';
}
