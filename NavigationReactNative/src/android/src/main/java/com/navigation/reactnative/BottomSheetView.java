package com.navigation.reactnative;

import android.content.Context;
import android.view.ViewGroup;

import androidx.coordinatorlayout.widget.CoordinatorLayout;

import com.google.android.material.bottomsheet.BottomSheetBehavior;

public class BottomSheetView extends ViewGroup {
    private BottomSheetBehavior bottomSheetBehavior = new BottomSheetBehavior();

    public BottomSheetView(Context context) {
        super(context);
        CoordinatorLayout.LayoutParams params = new CoordinatorLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        params.setBehavior(bottomSheetBehavior);
        setLayoutParams(params);
    }

    void setPeekHeight(int peekHeight) {
        bottomSheetBehavior.setPeekHeight(peekHeight);
    }

    void setExpandedOffset(int expandedOffset) {
        bottomSheetBehavior.setExpandedOffset(expandedOffset);
    }

    void setFitToContents(boolean fitToContents) {
        bottomSheetBehavior.setFitToContents(fitToContents);
    }

    @Override
    protected void onLayout(boolean b, int i, int i1, int i2, int i3) {
    }
}
