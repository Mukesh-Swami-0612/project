import { Component, Input } from '@angular/core';

export type LogoSize = 'nav' | 'auth' | 'splash';

@Component({
  selector: 'app-logo',
  standalone: true,
  templateUrl: './logo.component.html',
  styleUrl: './logo.component.css'
})
export class LogoComponent {
  @Input() size: LogoSize = 'nav';
  @Input() alt = 'CapKart';

  readonly src = 'assets/images/logo-optimized.png';
  imageFailed = false;
}
