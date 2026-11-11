import { Injectable } from '@angular/core';

export interface AppSettings {
  catalogApiUrl: string;
  ordersApiUrl: string;
}

/**
 * Загружает /config.json при старте приложения.
 * Так один и тот же собранный фронтенд работает локально, в Static Web Apps и в Kubernetes —
 * меняется только config.json. Пустой URL означает «тот же хост» (через прокси/nginx).
 */
@Injectable({ providedIn: 'root' })
export class ConfigService {
  private settings: AppSettings = { catalogApiUrl: '', ordersApiUrl: '' };

  get catalogApiUrl(): string {
    return this.settings.catalogApiUrl.replace(/\/$/, '');
  }

  get ordersApiUrl(): string {
    return this.settings.ordersApiUrl.replace(/\/$/, '');
  }

  async load(): Promise<void> {
    const response = await fetch('/config.json', { cache: 'no-store' });
    if (response.ok) {
      this.settings = { ...this.settings, ...(await response.json()) };
    }
  }
}
