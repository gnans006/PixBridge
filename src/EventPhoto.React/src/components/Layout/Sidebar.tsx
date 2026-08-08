import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronDown, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { authStore } from '../../store/authStore';
import { NAVIGATION, hasMinRole, type NavSection } from '../../config/navigation.config';
import { useApplicationSettings } from '../../hooks/useApplicationSettings';

interface SidebarProps {
  open?: boolean;
  collapsed?: boolean;
  onClose?: () => void;
  onToggleCollapse?: () => void;
}

function SectionGroup({
  section, collapsed, forceCollapsed, onClose,
}: { section: NavSection; collapsed: boolean; forceCollapsed?: boolean; onClose?: () => void }) {
  const [expanded, setExpanded] = useState(true);
  const user = authStore.getUser();
  const role = user?.role ?? 'Viewer';

  const visibleItems = section.items.filter(
    item => !item.minRole || hasMinRole(role, item.minRole),
  );
  if (visibleItems.length === 0) return null;

  return (
    <div className="mb-1">
      {!collapsed && (
        <button
          onClick={() => setExpanded(e => !e)}
          className="flex w-full items-center justify-between px-3 py-2 text-xs font-bold tracking-[0.07em] uppercase text-pds-text-2 hover:text-pds-text transition-colors"
        >
          {section.label}
          <motion.div animate={{ rotate: expanded ? 0 : -90 }} transition={{ duration: 0.2 }}>
            <ChevronDown className="h-3 w-3" />
          </motion.div>
        </button>
      )}

      <AnimatePresence initial={false}>
        {((expanded && !forceCollapsed) || collapsed) && (
          <motion.nav
            key="items"
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2, ease: 'easeInOut' }}
            className={`overflow-hidden ${collapsed ? 'px-1' : 'mt-0.5 space-y-0.5 px-2'}`}
          >
            {visibleItems.map(item => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                onClick={onClose}
                title={collapsed ? item.label : undefined}
                className={({ isActive }) =>
                  `group relative flex items-center rounded-lg transition-all duration-150 ${
                    collapsed ? 'my-0.5 h-10 w-10 justify-center' : 'gap-2.5 px-3 py-2 text-sm font-medium'
                  } ${
                    isActive
                      ? 'bg-pds-primary/10 text-pds-primary'
                      : 'text-pds-text-muted hover:bg-pds-elevated hover:text-pds-text-2'
                  }`
                }
              >
                {({ isActive }) => (
                  <>
                    {isActive && !collapsed && (
                      <motion.span
                        layoutId={`indicator-${section.id}`}
                        className="absolute left-0 top-1/2 -translate-y-1/2 h-5 w-0.5 rounded-r-full bg-pds-primary"
                        transition={{ type: 'spring', stiffness: 400, damping: 30 }}
                      />
                    )}
                    <item.icon className={`flex-shrink-0 ${collapsed ? 'h-[18px] w-[18px]' : 'h-4 w-4'}`} />
                    {!collapsed && <span className="truncate">{item.label}</span>}
                    {collapsed && (
                      <span className="pointer-events-none absolute left-full ml-2 hidden whitespace-nowrap rounded-lg border border-pds-border bg-pds-elevated px-2.5 py-1.5 text-xs font-medium text-pds-text shadow-pds-modal group-hover:block z-50">
                        {item.label}
                      </span>
                    )}
                  </>
                )}
              </NavLink>
            ))}
          </motion.nav>
        )}
      </AnimatePresence>
    </div>
  );
}

export function Sidebar({ open, collapsed = false, onClose, onToggleCollapse }: SidebarProps) {
  const [sectionsCollapsed, setSectionsCollapsed] = useState(false);
  const user = authStore.getUser();
  const role = user?.role ?? 'Viewer';
  const { data: appSettings } = useApplicationSettings();
  const studioName = appSettings?.studioName ?? 'PixBridge';

  const visibleSections = NAVIGATION.filter(
    section => !section.allowedRoles || section.allowedRoles.includes(role as never),
  );

  return (
    <aside
      className={[
        'z-30 flex flex-col flex-shrink-0 bg-pds-surface border-r border-pds-border',
        'absolute md:relative',
        'transition-all duration-300 ease-in-out',
        open
          ? 'translate-x-0 overflow-y-auto shadow-pds-modal md:shadow-none'
          : '-translate-x-full overflow-hidden md:translate-x-0',
        collapsed ? 'md:w-[60px]' : 'md:w-60',
        'w-60',
      ].join(' ')}
    >
      {/* Nav sections */}
      <div className={`relative flex-1 overflow-y-auto scrollbar-none ${collapsed ? 'py-3' : 'pt-8 pb-3'}`}>
        {/* Floating collapse pill — hangs above the first section */}
        {!collapsed && (
          <div className="absolute top-1.5 left-0 right-0 flex justify-center z-10">
            <button
              onClick={() => setSectionsCollapsed(s => !s)}
              title={sectionsCollapsed ? 'Expand all sections' : 'Collapse all sections'}
              className="flex items-center gap-1 rounded-full border border-pds-border bg-pds-elevated px-3 py-0.5 text-[11px] font-medium text-pds-text-muted shadow-pds-card hover:border-pds-primary/40 hover:bg-pds-primary/10 hover:text-pds-primary transition-all duration-150"
            >
              {sectionsCollapsed
                ? <><ChevronsRight className="h-3 w-3" /> Expand menu</>
                : <><ChevronsLeft className="h-3 w-3" /> Collapse menu</>}
            </button>
          </div>
        )}
        {visibleSections.map(section => (
          <SectionGroup key={section.id} section={section} collapsed={collapsed} forceCollapsed={sectionsCollapsed} onClose={onClose} />
        ))}
      </div>

      {/* Status */}
      <div className={`flex-shrink-0 border-t border-pds-border py-2.5 ${collapsed ? 'flex justify-center' : 'px-4'}`}>
        <div className="flex items-center gap-2">
          <span className="h-1.5 w-1.5 rounded-full bg-pds-success animate-pulse flex-none" />
          {!collapsed && <span className="text-[10px] text-pds-text-muted">System Online</span>}
        </div>
      </div>
    </aside>
  );
}

