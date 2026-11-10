import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ConfigError } from './config/config-error';
import { RuntimeConfigService } from './config/runtime-config';
import { TopBar } from './layout/top-bar';

/** The app shell: top bar plus the page for the current route. */
@Component({
  selector: 'app-root',
  imports: [ConfigError, TopBar, RouterOutlet],
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly config = inject(RuntimeConfigService);
}
