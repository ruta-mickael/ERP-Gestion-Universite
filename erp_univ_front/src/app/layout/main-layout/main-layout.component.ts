import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HlmSidebarImports } from '@spartan-ng/helm/sidebar';
import { SideBarComponent } from '../sidebar/side-bar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [HlmSidebarImports, SideBarComponent, RouterOutlet],
  templateUrl: './main-layout.component.html'
})
export class MainLayoutComponent {}
