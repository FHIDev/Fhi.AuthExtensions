import { Component, Input } from '@angular/core';
import { FhiPopoverMenuComponent } from '@folkehelseinstituttet/angular-components';

@Component({
    selector: 'app-nav-menu',
    templateUrl: './nav-menu.component.html',
    imports: [FhiPopoverMenuComponent]
})

export class NavMenuComponent {

  constructor(){
  }

  popoverMenuItems = [
    {
      icon: 'plus',
      name: 'Log in',
      link: {
        href: '/login',
      }
    },
    {
      icon: '',
      name: 'Log out',
      link: {
        href: '/logout',
      }
    },
    
  ];

  action(action: string) {
  }
}
