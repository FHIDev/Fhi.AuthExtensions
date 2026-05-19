/// <reference types="@angular/localize" />

import { bootstrapApplication } from '@angular/platform-browser';

import '@folkehelseinstituttet/designsystem';

import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

bootstrapApplication(AppComponent, appConfig).catch((err) =>
  console.error(err),
);
