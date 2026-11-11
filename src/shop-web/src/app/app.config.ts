import { inject, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, ApplicationConfig } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { ConfigService } from './config.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    provideAppInitializer(() => inject(ConfigService).load())
  ]
};
