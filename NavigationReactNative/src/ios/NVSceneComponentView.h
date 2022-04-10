#ifdef RCT_NEW_ARCH_ENABLED
#import <UIKit/UIKit.h>
#import <React/RCTViewComponentView.h>
#import "NVSceneController.h"


NS_ASSUME_NONNULL_BEGIN

@interface NVSceneComponentView : RCTViewComponentView <NVScene>

@property (nonatomic, copy) NSString *sceneKey;
@property (nonatomic, assign) NSInteger crumb;

@end

NS_ASSUME_NONNULL_END
#endif
