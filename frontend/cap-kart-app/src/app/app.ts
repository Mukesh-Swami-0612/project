import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SplashComponent } from './features/splash/splash.component';
import { ToastContainerComponent } from './shared/components/toast-container/toast-container.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SplashComponent, ToastContainerComponent],
  template: `
    <app-splash *ngIf="showSplash" (splashDone)="onSplashDone()"></app-splash>
    <div class="app-content" [class.app-visible]="!showSplash">
      <router-outlet></router-outlet>
    </div>
    <app-toast-container></app-toast-container>
  `,
  styles: [`
    .app-content {
      opacity: 0;
      transition: opacity 0.5s ease;
    }
    .app-content.app-visible {
      opacity: 1;
    }
  `]
})
export class App implements OnInit {
  private readonly splashStorageKey = 'capkart:splash-shown';

  showSplash = false;
  title = 'cap-kart-app';

  ngOnInit(): void {
    this.showSplash = sessionStorage.getItem(this.splashStorageKey) !== 'true';
  }

  onSplashDone(): void {
    sessionStorage.setItem(this.splashStorageKey, 'true');
    this.showSplash = false;
  }
}
